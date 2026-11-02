// <copyright file="GkzMailSender.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Net;
using System.Net.Mail;
using System.Threading;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sends the e-mails of the player panel (recovering a password, a changed e-mail address)
/// from soporte@mugkz.com, through the SMTP relay of Brevo.
/// </summary>
/// <remarks>
/// The access comes from environment variables and never from the repository, which is public.
/// Without <see cref="UserVariableName"/> and <see cref="PasswordVariableName"/>, nothing is sent.
/// </remarks>
public class GkzMailSender
{
    /// <summary>The environment variable with the SMTP host, by default the relay of Brevo.</summary>
    public const string HostVariableName = "GKZ_SMTP_HOST";

    /// <summary>The environment variable with the SMTP port, by default 587 (STARTTLS).</summary>
    public const string PortVariableName = "GKZ_SMTP_PORT";

    /// <summary>The environment variable with the SMTP user.</summary>
    public const string UserVariableName = "GKZ_SMTP_USER";

    /// <summary>The environment variable with the SMTP password (the SMTP key of Brevo).</summary>
    public const string PasswordVariableName = "GKZ_SMTP_PASSWORD";

    /// <summary>The environment variable with the sender address, by default soporte@mugkz.com.</summary>
    public const string FromVariableName = "GKZ_SMTP_FROM";

    private readonly ILogger<GkzMailSender> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string? _user;
    private readonly string? _password;
    private readonly string _from;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzMailSender"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GkzMailSender(ILogger<GkzMailSender> logger)
    {
        this._logger = logger;
        this._host = Variable(HostVariableName) ?? "smtp-relay.brevo.com";
        this._port = int.TryParse(Variable(PortVariableName), out var port) ? port : 587;
        this._user = Variable(UserVariableName);
        this._password = Variable(PasswordVariableName);
        this._from = Variable(FromVariableName) ?? "soporte@mugkz.com";
        if (!this.IsConfigured)
        {
            this._logger.LogWarning("{User} or {Password} isn't set: the player panel can't send e-mails.", UserVariableName, PasswordVariableName);
        }
    }

    /// <summary>
    /// Gets a value indicating whether e-mails can be sent.
    /// </summary>
    public bool IsConfigured => this._user is not null && this._password is not null;

    /// <summary>
    /// Sends a plain text e-mail.
    /// </summary>
    /// <param name="to">The address of the recipient.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="body">The text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if it was sent.</returns>
    public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (!this.IsConfigured)
        {
            return false;
        }

        try
        {
            using var message = new MailMessage(new MailAddress(this._from, "Soporte MU GKZ"), new MailAddress(to))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
            };
            using var client = new SmtpClient(this._host, this._port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(this._user, this._password),
                Timeout = 15000,
            };
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException)
        {
            this._logger.LogWarning(ex, "An e-mail of the player panel couldn't be sent.");
            return false;
        }
    }

    private static string? Variable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
