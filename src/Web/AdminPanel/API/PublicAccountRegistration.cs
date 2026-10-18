// <copyright file="PublicAccountRegistration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// MU GKZ: the data of a new account from the public website, with the token of the captcha.
/// </summary>
public class PublicAccountRegistration : AccountRegistration
{
    /// <summary>
    /// Gets or sets the token of the Cloudflare Turnstile widget.
    /// </summary>
    [Required]
    [MaxLength(GkzTurnstileVerifier.MaximumTokenLength)]
    public string CaptchaToken { get; set; } = string.Empty;
}
