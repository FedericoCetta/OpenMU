// <copyright file="GkzTurnstileVerifier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

/// <summary>
/// MU GKZ: verifies the Cloudflare Turnstile tokens of the registration form of the website.
/// </summary>
/// <remarks>
/// The secret key comes from the environment variable <see cref="SecretVariableName"/> and never from the
/// repository, which is public. Without it, every token is rejected: the public registration stays closed
/// instead of open to bots. The token must also come from the expected hostname and the action
/// <see cref="ExpectedAction"/> of the form.
/// </remarks>
public class GkzTurnstileVerifier
{
    /// <summary>
    /// The environment variable with the secret key of the Turnstile widget.
    /// </summary>
    public const string SecretVariableName = "GKZ_TURNSTILE_SECRET";

    /// <summary>
    /// The environment variable with the hostname of the website, by default <c>mugkz.com</c>.
    /// </summary>
    public const string HostnameVariableName = "GKZ_TURNSTILE_HOSTNAME";

    /// <summary>
    /// The action which the registration form sets on its widget.
    /// </summary>
    public const string ExpectedAction = "registro";

    /// <summary>
    /// The maximum length of a token, as documented by Cloudflare.
    /// </summary>
    public const int MaximumTokenLength = 2048;

    private static readonly Uri SiteVerifyUri = new("https://challenges.cloudflare.com/turnstile/v0/siteverify");

    private readonly HttpClient _httpClient;
    private readonly ILogger<GkzTurnstileVerifier> _logger;
    private readonly string? _secret;
    private readonly string _hostname;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzTurnstileVerifier"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GkzTurnstileVerifier(ILogger<GkzTurnstileVerifier> logger)
        : this(
            new HttpClient { Timeout = TimeSpan.FromSeconds(10) },
            logger,
            Environment.GetEnvironmentVariable(SecretVariableName),
            Environment.GetEnvironmentVariable(HostnameVariableName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzTurnstileVerifier"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="secret">The secret key.</param>
    /// <param name="hostname">The expected hostname.</param>
    public GkzTurnstileVerifier(HttpClient httpClient, ILogger<GkzTurnstileVerifier> logger, string? secret, string? hostname)
    {
        this._httpClient = httpClient;
        this._logger = logger;
        this._secret = string.IsNullOrWhiteSpace(secret) ? null : secret.Trim();
        this._hostname = string.IsNullOrWhiteSpace(hostname) ? "mugkz.com" : hostname.Trim();
        if (this._secret is null)
        {
            this._logger.LogWarning("{Variable} isn't set: the public registration rejects every request.", SecretVariableName);
        }
    }

    /// <summary>
    /// Verifies a token with Cloudflare.
    /// </summary>
    /// <param name="token">The token of the widget.</param>
    /// <param name="remoteIp">The IP address of the visitor, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if the token is valid for the registration form.</returns>
    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
    {
        if (this._secret is null || string.IsNullOrEmpty(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = this._secret,
            ["response"] = token,
        };
        if (!string.IsNullOrEmpty(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        try
        {
            using var response = await this._httpClient.PostAsync(SiteVerifyUri, new FormUrlEncodedContent(form), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Turnstile answered {Status}.", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<SiteVerifyResult>(cancellationToken).ConfigureAwait(false);
            if (result is not { Success: true })
            {
                this._logger.LogInformation("Turnstile rejected a registration: {Errors}.", string.Join(", ", result?.ErrorCodes ?? []));
                return false;
            }

            if (!string.Equals(result.Hostname, this._hostname, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(result.Action, ExpectedAction, StringComparison.Ordinal))
            {
                this._logger.LogWarning("Turnstile token of another hostname or action: {Hostname}, {Action}.", result.Hostname, result.Action);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            this._logger.LogWarning(ex, "Turnstile couldn't be reached.");
            return false;
        }
    }

    /// <summary>
    /// The answer of the siteverify endpoint.
    /// </summary>
    /// <param name="Success">Whether the token is valid.</param>
    /// <param name="Hostname">The hostname of the page with the widget.</param>
    /// <param name="Action">The action of the widget.</param>
    /// <param name="ErrorCodes">The error codes.</param>
    private sealed record SiteVerifyResult(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("hostname")] string? Hostname,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}
