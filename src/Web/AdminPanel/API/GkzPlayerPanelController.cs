// <copyright file="GkzPlayerPanelController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// The player panel of the MU GKZ website: a player logs in with the account of the game, sees
/// the account and its characters, and changes the password or the e-mail address, moves a stuck
/// character to Lorencia or recovers a forgotten password by e-mail.
/// </summary>
/// <remarks>
/// <para>
/// It's anonymous for ASP.NET, because the players have no account of the admin panel: it checks
/// its own session, a random token in an HttpOnly, Secure, SameSite=Strict cookie (see
/// <see cref="GkzPanelSessions"/>). Every POST also needs the header <see cref="RequestHeader"/>,
/// which another site can't send without CORS, so no other site can act in the name of a player.
/// </para>
/// <para>
/// A login needs a captcha and is blocked for a while after too many failures, per login name and
/// per IP address. The changes need the account to be out of the game, because the game server
/// keeps the account and its characters in memory while it's connected, and would save its own
/// values over them.
/// </para>
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/panel")]
public class GkzPlayerPanelController : ControllerBase
{
    /// <summary>
    /// The header which every POST of the panel needs.
    /// </summary>
    public const string RequestHeader = "X-GKZ-Panel";

    /// <summary>
    /// The name of the session cookie.
    /// </summary>
    public const string CookieName = "gkz_panel";

    /// <summary>
    /// The path of the panel in the website, where nginx forwards to this controller.
    /// </summary>
    public const string PublicPath = "/public/panel";

    /// <summary>
    /// The page of the website with the panel; the recovery links point to it.
    /// </summary>
    public const string PanelPage = "https://mugkz.com/panel.html";

    // A valid hash, so that a login with an unknown name takes as long as one with a wrong password.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    // A new password: like the registration form of the website, 6 to 20 printable ASCII characters without spaces.
    private static readonly Regex NewPasswordPattern = new(@"^[\x21-\x7E]{6,20}$", RegexOptions.Compiled);

    private static readonly Regex LoginNamePattern = new("^[A-Za-z0-9]+$", RegexOptions.Compiled);

    private readonly IGkzPlayerPanelQueries _queries;
    private readonly GkzPanelSessions _sessions;
    private readonly IDictionary<int, IGameServer> _gameServers;
    private readonly ILogger<GkzPlayerPanelController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzPlayerPanelController"/> class.
    /// </summary>
    /// <param name="queries">The queries.</param>
    /// <param name="sessions">The sessions.</param>
    /// <param name="gameServers">The game servers.</param>
    /// <param name="logger">The logger.</param>
    public GkzPlayerPanelController(
        IGkzPlayerPanelQueries queries,
        GkzPanelSessions sessions,
        IDictionary<int, IGameServer> gameServers,
        ILogger<GkzPlayerPanelController> logger)
    {
        this._queries = queries;
        this._sessions = sessions;
        this._gameServers = gameServers;
        this._logger = logger;
    }

    private string RemoteIp => this.Request.Headers["X-Real-IP"].ToString();

    /// <summary>
    /// Logs a player in.
    /// </summary>
    /// <param name="request">The login name, password and captcha token.</param>
    /// <param name="verifier">The verifier of the captcha.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 with the session cookie; 401 for wrong data; 403 for a suspended account or an invalid captcha; 429 when blocked.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, [FromServices] GkzTurnstileVerifier verifier, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        var loginName = request.LoginName?.Trim() ?? string.Empty;
        var nameKey = "n:" + loginName.ToLowerInvariant();
        var ipKey = "i:" + this.RemoteIp;
        if (this._sessions.IsBlocked(nameKey) || this._sessions.IsBlocked(ipKey))
        {
            return Error(StatusCodes.Status429TooManyRequests, "Demasiados intentos fallidos. Esperá 15 minutos y probá de nuevo.");
        }

        if (!await verifier.VerifyAsync(request.CaptchaToken, this.RemoteIp, cancellationToken, GkzTurnstileVerifier.PanelAction).ConfigureAwait(false))
        {
            return Error(StatusCodes.Status403Forbidden, "No pudimos verificar que no seas un robot. Probá de nuevo.");
        }

        var password = request.Password ?? string.Empty;
        var account = IsValidLoginName(loginName) && password.Length <= AccountRegistration.PasswordMaximumLength
            ? await this._queries.GetAccountAsync(loginName, cancellationToken).ConfigureAwait(false)
            : null;
        var isValid = BCrypt.Net.BCrypt.Verify(password, account?.PasswordHash is { Length: > 0 } hash ? hash : DummyHash);
        if (account is null || account.IsBotOrTemplate || !isValid)
        {
            this._sessions.AddFailure(nameKey);
            this._sessions.AddFailure(ipKey);
            return Error(StatusCodes.Status401Unauthorized, "La cuenta o la contraseña no son correctas.");
        }

        if (IsSuspended(account))
        {
            return Error(StatusCodes.Status403Forbidden, "La cuenta está suspendida. Escribinos a soporte@mugkz.com.");
        }

        this._sessions.ClearFailures(nameKey);
        this.SetSessionCookie(this._sessions.CreateSession(account.Id, account.LoginName));
        this._logger.LogInformation("Player panel: {LoginName} logged in from {Ip}.", account.LoginName, this.RemoteIp);
        return this.Ok(new { loginName = account.LoginName });
    }

    /// <summary>
    /// Logs the player out.
    /// </summary>
    /// <returns>204.</returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        this._sessions.EndSession(this.Request.Cookies[CookieName]);
        this.Response.Cookies.Delete(CookieName, new CookieOptions { Path = PublicPath, Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict });
        return this.NoContent();
    }

    /// <summary>
    /// Gets the account of the logged in player, with its characters.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 with the account; 401 without a session.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMeAsync(CancellationToken cancellationToken)
    {
        this.Response.Headers.CacheControl = "no-store";
        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        var characters = await this._queries.GetCharactersAsync(account.Id, cancellationToken).ConfigureAwait(false);
        var purchases = await this._queries.GetPurchasesAsync(account.Id, cancellationToken).ConfigureAwait(false);
        var online = await this.GetOnlineCharacterAsync(account.LoginName).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        return this.Ok(new
        {
            loginName = account.LoginName,
            email = account.EMail,
            registered = DateTime.SpecifyKind(account.RegistrationDate, DateTimeKind.Utc),
            vipUntil = account.VipUntil is { } vip && vip > now ? DateTime.SpecifyKind(vip, DateTimeKind.Utc) : (DateTime?)null,
            coins = account.Coins + account.PendingCoins,
            pendingCoins = account.PendingCoins,
            online = online.IsOnline,
            characters = characters.Select(c => new
            {
                name = c.Name,
                @class = GkzPublicWebController.ClassNames.GetValueOrDefault(c.ClassNumber, string.Empty),
                level = c.Level,
                resets = c.Resets,
                map = MapName(c.Map),
                kills = c.Kills,
                guild = c.Guild,
                online = online.Character == c.Name,
            }),
            packs = GkzCoinPacks.All.Select(p => new { id = p.Id, name = p.Name, coins = p.Coins, bonus = p.BonusCoins, price = p.PriceUsd }),
            purchases = purchases.Select(p => new
            {
                description = p.Reason,
                coins = p.Amount,
                date = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc),
                applied = p.IsApplied,
            }),
        });
    }

    /// <summary>
    /// Starts the purchase of a pack of GKZ Coins with Mercado Pago.
    /// </summary>
    /// <param name="request">The pack.</param>
    /// <param name="mercadoPago">The client of Mercado Pago.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 with the address of the payment page; 400 for an unknown pack; 503 when Mercado Pago isn't available.</returns>
    [HttpPost("buy")]
    public async Task<IActionResult> BuyAsync([FromBody] BuyRequest request, [FromServices] GkzMercadoPago mercadoPago, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        if (GkzCoinPacks.Find(request.Pack) is not { } pack)
        {
            return Error(StatusCodes.Status400BadRequest, "Ese pack no existe.");
        }

        if (await mercadoPago.CreatePaymentPageAsync(account.Id, account.LoginName, pack, cancellationToken).ConfigureAwait(false) is not { } page)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, "Por ahora no podemos cobrar con Mercado Pago. Probá en un rato.");
        }

        this._logger.LogInformation("Player panel: {LoginName} started buying the pack {Pack}.", account.LoginName, pack.Id);
        return this.Ok(new { url = page });
    }

    /// <summary>
    /// Checks a payment of Mercado Pago when the player comes back from the payment page, so that the
    /// coins don't wait for the notification. It grants the coins only once, like the notification.
    /// </summary>
    /// <param name="request">The id of the payment.</param>
    /// <param name="mercadoPago">The client of Mercado Pago.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 with the status of the payment; 404 if it isn't a payment of this account.</returns>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmAsync([FromBody] ConfirmRequest request, [FromServices] GkzMercadoPago mercadoPago, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        var result = await mercadoPago.ProcessPaymentAsync(request.PaymentId ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (result.Status == GkzPaymentStatus.Error)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, "No pudimos consultar el pago. Si lo pagaste, las coins llegan solas en unos minutos.");
        }

        if (result.AccountId != account.Id || result.Pack is not { } pack)
        {
            return Error(StatusCodes.Status404NotFound, "No encontramos ese pago en tu cuenta.");
        }

        var status = result.Status switch
        {
            GkzPaymentStatus.Credited or GkzPaymentStatus.AlreadyCredited => "acreditado",
            GkzPaymentStatus.Pending => "pendiente",
            GkzPaymentStatus.Refunded => "devuelto",
            _ => "rechazado",
        };
        return this.Ok(new { status, coins = pack.TotalCoins, pack = pack.Name });
    }

    /// <summary>
    /// Changes the password of the logged in player.
    /// </summary>
    /// <param name="request">The current and the new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204; 400 for an invalid new password; 401 for a wrong current password; 409 while in the game.</returns>
    [HttpPost("password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordChangeRequest request, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        if (!IsValidNewPassword(request.NewPassword))
        {
            return Error(StatusCodes.Status400BadRequest, "La contraseña nueva tiene que tener de 6 a 20 caracteres, sin espacios, acentos ni ñ.");
        }

        if (!this.CheckPassword(account, request.CurrentPassword))
        {
            return Error(StatusCodes.Status401Unauthorized, "La contraseña actual no es correcta.");
        }

        if ((await this.GetOnlineCharacterAsync(account.LoginName).ConfigureAwait(false)).IsOnline)
        {
            return InGame();
        }

        await this._queries.SetPasswordHashAsync(account.Id, BCrypt.Net.BCrypt.HashPassword(request.NewPassword!), cancellationToken).ConfigureAwait(false);
        this._sessions.EndSessionsOf(account.Id, this.Request.Cookies[CookieName]);
        this._logger.LogInformation("Player panel: {LoginName} changed the password.", account.LoginName);
        return this.NoContent();
    }

    /// <summary>
    /// Changes the e-mail address of the logged in player; the previous address gets a notice.
    /// </summary>
    /// <param name="request">The password and the new address.</param>
    /// <param name="mailSender">The sender of e-mails.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204; 400 for an invalid address; 401 for a wrong password; 409 while in the game.</returns>
    [HttpPost("email")]
    public async Task<IActionResult> ChangeEMailAsync([FromBody] EMailChangeRequest request, [FromServices] GkzMailSender mailSender, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        var email = request.EMail?.Trim() ?? string.Empty;
        if (email.Length is 0 or > 254 || !new EmailAddressAttribute().IsValid(email))
        {
            return Error(StatusCodes.Status400BadRequest, "El correo no es válido.");
        }

        if (!this.CheckPassword(account, request.Password))
        {
            return Error(StatusCodes.Status401Unauthorized, "La contraseña no es correcta.");
        }

        if ((await this.GetOnlineCharacterAsync(account.LoginName).ConfigureAwait(false)).IsOnline)
        {
            return InGame();
        }

        await this._queries.SetEMailAsync(account.Id, email, cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Player panel: {LoginName} changed the e-mail address.", account.LoginName);
        if (!string.IsNullOrWhiteSpace(account.EMail) && !string.Equals(account.EMail, email, StringComparison.OrdinalIgnoreCase))
        {
            var notice = $"Hola,\n\nEl correo de la cuenta {account.LoginName} de MU GKZ cambió a {Mask(email)}.\n\n"
                + "Si fuiste vos, no tenés que hacer nada. Si no fuiste vos, escribinos enseguida respondiendo este correo.\n\n"
                + "Soporte MU GKZ\nhttps://mugkz.com";
            await mailSender.SendAsync(account.EMail, "MU GKZ: cambió el correo de tu cuenta", notice, cancellationToken).ConfigureAwait(false);
        }

        return this.NoContent();
    }

    /// <summary>
    /// Moves a character of the logged in player to the safe zone of Lorencia.
    /// </summary>
    /// <param name="request">The name of the character.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204; 404 for an unknown character; 409 while in the game.</returns>
    [HttpPost("unstick")]
    public async Task<IActionResult> UnstickAsync([FromBody] UnstickRequest request, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (await this.GetAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return this.NotLoggedIn();
        }

        var name = request.Character ?? string.Empty;
        if (name.Length is 0 or > 10)
        {
            return Error(StatusCodes.Status404NotFound, "Ese personaje no es de tu cuenta.");
        }

        if ((await this.GetOnlineCharacterAsync(account.LoginName).ConfigureAwait(false)).IsOnline)
        {
            return InGame();
        }

        if (!await this._queries.MoveToLorenciaAsync(account.Id, name, cancellationToken).ConfigureAwait(false))
        {
            return Error(StatusCodes.Status404NotFound, "Ese personaje no es de tu cuenta.");
        }

        this._logger.LogInformation("Player panel: {LoginName} moved {Character} to Lorencia.", account.LoginName, name);
        return this.NoContent();
    }

    /// <summary>
    /// Sends a link to recover the password to the e-mail address of an account. The answer is
    /// the same whether the account exists or not, so it doesn't tell which accounts exist.
    /// </summary>
    /// <param name="request">The login name and captcha token.</param>
    /// <param name="verifier">The verifier of the captcha.</param>
    /// <param name="mailSender">The sender of e-mails.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204; 403 for an invalid captcha; 503 when e-mails can't be sent.</returns>
    [HttpPost("recover")]
    public async Task<IActionResult> RecoverAsync(
        [FromBody] RecoverRequest request,
        [FromServices] GkzTurnstileVerifier verifier,
        [FromServices] GkzMailSender mailSender,
        CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (!mailSender.IsConfigured)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, "Por ahora no podemos mandar correos. Escribinos a soporte@mugkz.com.");
        }

        if (!await verifier.VerifyAsync(request.CaptchaToken, this.RemoteIp, cancellationToken, GkzTurnstileVerifier.PanelAction).ConfigureAwait(false))
        {
            return Error(StatusCodes.Status403Forbidden, "No pudimos verificar que no seas un robot. Probá de nuevo.");
        }

        var loginName = request.LoginName?.Trim() ?? string.Empty;
        var account = IsValidLoginName(loginName) ? await this._queries.GetAccountAsync(loginName, cancellationToken).ConfigureAwait(false) : null;
        if (account is null || account.IsBotOrTemplate || IsSuspended(account) || string.IsNullOrWhiteSpace(account.EMail))
        {
            return this.NoContent();
        }

        if (this._sessions.CreateRecovery(account.Id) is not { } token)
        {
            return this.NoContent();
        }

        var body = $"Hola,\n\nPediste una contraseña nueva para la cuenta {account.LoginName} de MU GKZ. Entrá a este enlace para elegirla:\n\n"
            + $"{PanelPage}#recuperar={token}\n\n"
            + $"El enlace vale {(int)GkzPanelSessions.RecoveryLifetime.TotalMinutes} minutos y una sola vez. Si no lo pediste vos, ignorá este correo: tu contraseña no cambia.\n\n"
            + "Soporte MU GKZ\nhttps://mugkz.com";
        await mailSender.SendAsync(account.EMail, "MU GKZ: recuperá tu contraseña", body, cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Player panel: recovery e-mail for {LoginName}.", account.LoginName);
        return this.NoContent();
    }

    /// <summary>
    /// Sets a new password with the link of a recovery e-mail.
    /// </summary>
    /// <param name="request">The token of the link and the new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204; 400 for an invalid password or link; 409 while in the game.</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetRequest request, CancellationToken cancellationToken)
    {
        if (!this.HasPanelHeader())
        {
            return this.BadRequest();
        }

        if (this._sessions.GetRecovery(request.Token) is not { } accountId
            || await this._queries.GetAccountAsync(accountId, cancellationToken).ConfigureAwait(false) is not { } account)
        {
            return Error(StatusCodes.Status400BadRequest, "El enlace no es válido o ya venció. Pedí uno nuevo.");
        }

        if (!IsValidNewPassword(request.Password))
        {
            return Error(StatusCodes.Status400BadRequest, "La contraseña nueva tiene que tener de 6 a 20 caracteres, sin espacios, acentos ni ñ.");
        }

        if ((await this.GetOnlineCharacterAsync(account.LoginName).ConfigureAwait(false)).IsOnline)
        {
            return InGame();
        }

        await this._queries.SetPasswordHashAsync(account.Id, BCrypt.Net.BCrypt.HashPassword(request.Password!), cancellationToken).ConfigureAwait(false);
        this._sessions.EndRecovery(request.Token!);
        this._sessions.EndSessionsOf(account.Id);
        this._sessions.ClearFailures("n:" + account.LoginName.ToLowerInvariant());
        this._logger.LogInformation("Player panel: {LoginName} set a new password with a recovery link.", account.LoginName);
        return this.NoContent();
    }

    private static bool IsValidLoginName(string loginName)
        => loginName.Length is >= AccountRegistration.MinimumLength and <= AccountRegistration.LoginNameMaximumLength
           && LoginNamePattern.IsMatch(loginName);

    private static bool IsValidNewPassword(string? password)
        => password is not null && NewPasswordPattern.IsMatch(password);

    private static bool IsSuspended(GkzPanelAccount account)
        => account.State is (int)AccountState.Banned or (int)AccountState.TemporarilyBanned;

    // The name of a map carries its translations after "||" (Noria||zh-CN=...).
    private static string MapName(string? name) => (name ?? string.Empty).Split("||")[0];

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" + email[Math.Max(at, 0)..] : email[0] + new string('*', at - 1) + email[at..];
    }

    private static ObjectResult Error(int status, string message)
        => new(new { error = message }) { StatusCode = status };

    private static ObjectResult InGame()
        => Error(StatusCodes.Status409Conflict, "Tu cuenta está conectada al juego. Salí del juego y probá de nuevo.");

    private ObjectResult NotLoggedIn()
        => Error(StatusCodes.Status401Unauthorized, "Tu sesión terminó. Entrá de nuevo.");

    private bool HasPanelHeader() => this.Request.Headers.ContainsKey(RequestHeader);

    private bool CheckPassword(GkzPanelAccount account, string? password)
        => password is { Length: > 0 and <= AccountRegistration.PasswordMaximumLength }
           && BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);

    private void SetSessionCookie(string token)
    {
        this.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            Path = PublicPath,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = GkzPanelSessions.MaximumLifetime,
        });
    }

    private async Task<GkzPanelAccount?> GetAccountAsync(CancellationToken cancellationToken)
    {
        if (this._sessions.GetSession(this.Request.Cookies[CookieName]) is not { } session)
        {
            return null;
        }

        var account = await this._queries.GetAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (account is null || IsSuspended(account))
        {
            this._sessions.EndSessionsOf(session.AccountId);
            return null;
        }

        return account;
    }

    private async Task<(bool IsOnline, string? Character)> GetOnlineCharacterAsync(string loginName)
    {
        foreach (var server in this._gameServers.Values.OfType<GameServer>())
        {
            var players = await server.Context.GetPlayersAsync().ConfigureAwait(false);
            if (players.FirstOrDefault(p => p.Account?.LoginName == loginName) is { } player)
            {
                return (true, player.SelectedCharacter?.Name);
            }
        }

        return (false, null);
    }

    /// <summary>The data of a login.</summary>
    /// <param name="LoginName">The login name.</param>
    /// <param name="Password">The password.</param>
    /// <param name="CaptchaToken">The token of the captcha.</param>
    public sealed record LoginRequest(string? LoginName, string? Password, string? CaptchaToken);

    /// <summary>The data of a password change.</summary>
    /// <param name="CurrentPassword">The current password.</param>
    /// <param name="NewPassword">The new password.</param>
    public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);

    /// <summary>The data of an e-mail change.</summary>
    /// <param name="Password">The password.</param>
    /// <param name="EMail">The new e-mail address.</param>
    public sealed record EMailChangeRequest(string? Password, string? EMail);

    /// <summary>The character to move to Lorencia.</summary>
    /// <param name="Character">The name of the character.</param>
    public sealed record UnstickRequest(string? Character);

    /// <summary>The data to recover a password.</summary>
    /// <param name="LoginName">The login name.</param>
    /// <param name="CaptchaToken">The token of the captcha.</param>
    public sealed record RecoverRequest(string? LoginName, string? CaptchaToken);

    /// <summary>The pack to buy.</summary>
    /// <param name="Pack">The id of the pack.</param>
    public sealed record BuyRequest(string? Pack);

    /// <summary>The payment to check.</summary>
    /// <param name="PaymentId">The id of the payment of Mercado Pago.</param>
    public sealed record ConfirmRequest(string? PaymentId);

    /// <summary>The data to set a new password with a recovery link.</summary>
    /// <param name="Token">The token of the link.</param>
    /// <param name="Password">The new password.</param>
    public sealed record ResetRequest(string? Token, string? Password);
}
