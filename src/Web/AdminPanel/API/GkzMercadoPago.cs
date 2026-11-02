// <copyright file="GkzMercadoPago.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// The result of checking a payment.
/// </summary>
public enum GkzPaymentStatus
{
    /// <summary>The payment is approved and its coins were granted now.</summary>
    Credited,

    /// <summary>The payment is approved and its coins were already granted before.</summary>
    AlreadyCredited,

    /// <summary>The payment isn't finished yet, e.g. a cash payment which wasn't paid yet.</summary>
    Pending,

    /// <summary>The payment was rejected or cancelled.</summary>
    Rejected,

    /// <summary>The payment was refunded or charged back, and its coins were taken back.</summary>
    Refunded,

    /// <summary>The payment isn't one of the panel, or its amount doesn't match the pack.</summary>
    Invalid,

    /// <summary>Mercado Pago couldn't be reached or isn't configured.</summary>
    Error,
}

/// <summary>
/// The result of checking a payment.
/// </summary>
/// <param name="Status">The status.</param>
/// <param name="AccountId">The account which bought the pack, if the payment is one of the panel.</param>
/// <param name="Pack">The bought pack, if the payment is one of the panel.</param>
public sealed record GkzPaymentResult(GkzPaymentStatus Status, Guid? AccountId = null, GkzCoinPack? Pack = null);

/// <summary>
/// MU GKZ: sells the packs of GKZ Coins with Checkout Pro of Mercado Pago (Preferences API).
/// </summary>
/// <remarks>
/// <para>
/// The player panel creates a preference for the account and the pack, and the player pays on the
/// page of Mercado Pago. The coins are only granted after asking the API of Mercado Pago for the
/// payment, with the own access token: neither the notification nor the return to the website is
/// trusted by itself. The payment must be approved, in US dollars and for the exact price of the pack.
/// </para>
/// <para>
/// The grant has the reference <c>mp:{payment id}</c>, so a payment grants its coins only once, however
/// often it's notified. A refund or chargeback adds a negative grant <c>mp:{payment id}:devolucion</c>.
/// The access token and the secret of the notifications come from the environment, never from the repository.
/// </para>
/// </remarks>
public class GkzMercadoPago
{
    /// <summary>
    /// The environment variable with the access token of the application.
    /// </summary>
    public const string AccessTokenVariableName = "GKZ_MP_ACCESS_TOKEN";

    /// <summary>
    /// The environment variable with the secret key of the notifications (webhooks).
    /// </summary>
    public const string WebhookSecretVariableName = "GKZ_MP_WEBHOOK_SECRET";

    /// <summary>
    /// The prefix of the references of the grants.
    /// </summary>
    public const string ReferencePrefix = "mp:";

    private const string Currency = "USD";
    private const string ExternalReferencePrefix = "gkz:";
    private const string NotificationUrl = "https://mugkz.com/public/pagos/mercadopago?source_news=webhooks";
    private const string ReturnUrl = GkzPlayerPanelController.PanelPage + "?pago=";

    private static readonly Uri ApiUri = new("https://api.mercadopago.com/");

    private readonly HttpClient _httpClient;
    private readonly CashShopCoinService _coinService;
    private readonly IGkzPlayerPanelQueries _queries;
    private readonly ILogger<GkzMercadoPago> _logger;
    private readonly string? _accessToken;
    private readonly byte[]? _webhookSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzMercadoPago"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The game configuration source.</param>
    /// <param name="queries">The queries of the player panel.</param>
    /// <param name="logger">The logger.</param>
    public GkzMercadoPago(
        IPersistenceContextProvider persistenceContextProvider,
        IDataSource<GameConfiguration> gameConfigurationSource,
        IGkzPlayerPanelQueries queries,
        ILogger<GkzMercadoPago> logger)
    {
        this._httpClient = new HttpClient { BaseAddress = ApiUri, Timeout = TimeSpan.FromSeconds(15) };
        this._coinService = new CashShopCoinService(persistenceContextProvider, gameConfigurationSource);
        this._queries = queries;
        this._logger = logger;
        this._accessToken = Environment.GetEnvironmentVariable(AccessTokenVariableName)?.Trim() is { Length: > 0 } token ? token : null;
        this._webhookSecret = Environment.GetEnvironmentVariable(WebhookSecretVariableName)?.Trim() is { Length: > 0 } secret
            ? Encoding.UTF8.GetBytes(secret)
            : null;
        if (this._accessToken is null)
        {
            this._logger.LogWarning("{Variable} isn't set: the player panel can't sell coins with Mercado Pago.", AccessTokenVariableName);
        }
        else
        {
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this._accessToken);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the access token is set.
    /// </summary>
    public bool IsConfigured => this._accessToken is not null;

    /// <summary>
    /// Creates a payment of a pack for an account.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="loginName">The login name of the account.</param>
    /// <param name="pack">The pack.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The address of the payment page; <c>null</c>, if Mercado Pago couldn't create it.</returns>
    public async Task<string?> CreatePaymentPageAsync(Guid accountId, string loginName, GkzCoinPack pack, CancellationToken cancellationToken)
    {
        if (!this.IsConfigured)
        {
            return null;
        }

        var preference = new
        {
            items = new[]
            {
                new
                {
                    id = pack.Id,
                    title = $"MU GKZ - {pack.Name}: {pack.TotalCoins} GKZ Coins",
                    description = $"{pack.TotalCoins} GKZ Coins para la cuenta {loginName}",
                    quantity = 1,
                    unit_price = pack.PriceUsd,
                    currency_id = Currency,
                },
            },
            external_reference = $"{ExternalReferencePrefix}{accountId:N}:{pack.Id}",
            notification_url = NotificationUrl,
            back_urls = new { success = ReturnUrl + "ok", pending = ReturnUrl + "pendiente", failure = ReturnUrl + "error" },
            auto_return = "approved",
            statement_descriptor = "MUGKZ",
        };

        try
        {
            using var response = await this._httpClient.PostAsJsonAsync("checkout/preferences", preference, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Mercado Pago didn't create a preference: {Status}.", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PreferenceResult>(cancellationToken).ConfigureAwait(false);
            return result?.InitPoint is { } page && page.StartsWith("https://", StringComparison.Ordinal) ? page : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            this._logger.LogWarning(ex, "Mercado Pago couldn't be reached to create a preference.");
            return null;
        }
    }

    /// <summary>
    /// Checks if a notification comes from Mercado Pago, by the signature with the secret key of the notifications.
    /// Without the secret key, every notification is accepted, because the payment is checked with the API anyway.
    /// </summary>
    /// <param name="signature">The header <c>x-signature</c> (<c>ts=...,v1=...</c>).</param>
    /// <param name="requestId">The header <c>x-request-id</c>.</param>
    /// <param name="dataId">The parameter <c>data.id</c> of the address.</param>
    /// <returns><c>true</c>, if the signature is valid or there is no secret key.</returns>
    public bool IsValidNotification(string? signature, string? requestId, string dataId)
    {
        if (this._webhookSecret is null)
        {
            return true;
        }

        string? ts = null;
        string? v1 = null;
        foreach (var part in (signature ?? string.Empty).Split(','))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            switch (pair[0].Trim())
            {
                case "ts":
                    ts = pair[1].Trim();
                    break;
                case "v1":
                    v1 = pair[1].Trim();
                    break;
            }
        }

        if (ts is null || v1 is null)
        {
            return false;
        }

        var manifest = new StringBuilder();
        manifest.Append("id:").Append(dataId.ToLowerInvariant()).Append(';');
        if (!string.IsNullOrEmpty(requestId))
        {
            manifest.Append("request-id:").Append(requestId).Append(';');
        }

        manifest.Append("ts:").Append(ts).Append(';');
        var expected = HMACSHA256.HashData(this._webhookSecret, Encoding.UTF8.GetBytes(manifest.ToString()));
        byte[] received;
        try
        {
            received = Convert.FromHexString(v1);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, received);
    }

    /// <summary>
    /// Asks Mercado Pago for a payment, and grants or takes back the coins of its pack.
    /// </summary>
    /// <param name="paymentId">The id of the payment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<GkzPaymentResult> ProcessPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        if (!this.IsConfigured || paymentId.Length is 0 or > 20 || !paymentId.All(char.IsAsciiDigit))
        {
            return new(this.IsConfigured ? GkzPaymentStatus.Invalid : GkzPaymentStatus.Error);
        }

        Payment? payment;
        try
        {
            using var response = await this._httpClient.GetAsync($"v1/payments/{paymentId}", cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new(GkzPaymentStatus.Invalid);
            }

            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Mercado Pago answered {Status} for the payment {PaymentId}.", response.StatusCode, paymentId);
                return new(GkzPaymentStatus.Error);
            }

            payment = await response.Content.ReadFromJsonAsync<Payment>(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            this._logger.LogWarning(ex, "Mercado Pago couldn't be reached for the payment {PaymentId}.", paymentId);
            return new(GkzPaymentStatus.Error);
        }

        if (payment is null || !TryParseExternalReference(payment.ExternalReference, out var accountId, out var pack))
        {
            return new(GkzPaymentStatus.Invalid);
        }

        var status = payment.Status switch
        {
            "approved" => GkzPaymentStatus.Credited,
            "refunded" or "charged_back" => GkzPaymentStatus.Refunded,
            "rejected" or "cancelled" => GkzPaymentStatus.Rejected,
            _ => GkzPaymentStatus.Pending,
        };
        if (status is GkzPaymentStatus.Pending or GkzPaymentStatus.Rejected)
        {
            return new(status, accountId, pack);
        }

        if (payment.CurrencyId != Currency || payment.TransactionAmount != pack.PriceUsd)
        {
            this._logger.LogWarning(
                "Mercado Pago payment {PaymentId} of {Amount} {Currency} doesn't match the pack {Pack} ({Price} USD).",
                paymentId,
                payment.TransactionAmount,
                payment.CurrencyId,
                pack.Id,
                pack.PriceUsd);
            return new(GkzPaymentStatus.Invalid, accountId, pack);
        }

        if (await this._queries.GetAccountAsync(accountId, cancellationToken).ConfigureAwait(false) is not { } account)
        {
            this._logger.LogWarning("Mercado Pago payment {PaymentId} is for the account {AccountId}, which doesn't exist.", paymentId, accountId);
            return new(GkzPaymentStatus.Invalid, accountId, pack);
        }

        // A refunded payment was approved before, so its coins are granted (once) before taking them back.
        var reference = ReferencePrefix + paymentId;
        var reason = string.Create(CultureInfo.InvariantCulture, $"Mercado Pago: {pack.Name} (USD {pack.PriceUsd})");
        var (grantStatus, _) = await this._coinService.GrantAsync(
            account.LoginName, CashShopCoinType.WCoinC, pack.TotalCoins, reason, reference, "Mercado Pago", cancellationToken).ConfigureAwait(false);
        if (grantStatus == CashShopCoinGrantStatus.ReferenceUsedByOtherAccount)
        {
            return new(GkzPaymentStatus.Invalid, accountId, pack);
        }

        if (status == GkzPaymentStatus.Refunded)
        {
            await this._coinService.GrantAsync(
                account.LoginName, CashShopCoinType.WCoinC, -pack.TotalCoins, $"{reason}: devuelto", reference + ":devolucion", "Mercado Pago", cancellationToken).ConfigureAwait(false);
            this._logger.LogWarning("Mercado Pago payment {PaymentId} of {LoginName} was {Status}: coins taken back.", paymentId, account.LoginName, payment.Status);
            return new(status, accountId, pack);
        }

        if (grantStatus == CashShopCoinGrantStatus.AlreadyGranted)
        {
            return new(GkzPaymentStatus.AlreadyCredited, accountId, pack);
        }

        this._logger.LogInformation("Mercado Pago payment {PaymentId}: {Coins} coins for {LoginName}.", paymentId, pack.TotalCoins, account.LoginName);
        return new(GkzPaymentStatus.Credited, accountId, pack);
    }

    private static bool TryParseExternalReference(string? externalReference, out Guid accountId, [NotNullWhen(true)] out GkzCoinPack? pack)
    {
        accountId = Guid.Empty;
        pack = null;
        var parts = externalReference?.Split(':');
        if (parts is not [_, var id, var packId] || externalReference!.StartsWith(ExternalReferencePrefix, StringComparison.Ordinal) is false)
        {
            return false;
        }

        pack = GkzCoinPacks.Find(packId);
        return Guid.TryParseExact(id, "N", out accountId) && pack is not null;
    }

    private sealed record PreferenceResult([property: JsonPropertyName("init_point")] string? InitPoint);

    private sealed record Payment(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("currency_id")] string? CurrencyId,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);
}
