// <copyright file="GkzWelcomeKitConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.WelcomeKit;

/// <summary>
/// The configuration of the <see cref="GkzWelcomeKitPlugIn"/>.
/// </summary>
public class GkzWelcomeKitConfiguration
{
    /// <summary>
    /// Gets or sets the date and time (UTC) from which new characters don't get the kit anymore.
    /// By default, 30 days after the opening of the 4th of November 2026, at 19:00 (UTC-3).
    /// </summary>
    [Display(Name = "Fin del kit (UTC)")]
    public DateTime EndUtc { get; set; } = new(2026, 12, 4, 22, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets or sets the free points of a new character.
    /// </summary>
    [Display(Name = "Puntos")]
    public int Points { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the zen of a new character.
    /// </summary>
    [Display(Name = "Zen")]
    public int Money { get; set; } = 5_000_000;

    /// <summary>
    /// Gets or sets how many days the wings last.
    /// </summary>
    [Display(Name = "Días de las alas")]
    public int WingsDays { get; set; } = 4;

    /// <summary>
    /// Gets or sets how many kits an account can get, so that creating and deleting characters doesn't give endless zen.
    /// </summary>
    [Display(Name = "Máximo de kits por cuenta")]
    public int MaximumKitsPerAccount { get; set; } = 5;
}
