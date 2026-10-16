// <copyright file="GkzPvpBalanceConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The base classes of MU GKZ, for the PvP balance.
/// </summary>
public enum GkzPvpClass
{
    /// <summary>Dark Wizard, Soul Master, Grand Master.</summary>
    [Display(Name = "Mago (DW/SM/GM)")]
    DarkWizard,

    /// <summary>Dark Knight, Blade Knight, Blade Master.</summary>
    [Display(Name = "Caballero (DK/BK/BM)")]
    DarkKnight,

    /// <summary>Fairy Elf, Muse Elf, High Elf.</summary>
    [Display(Name = "Elfa")]
    Elf,

    /// <summary>Magic Gladiator, Duel Master.</summary>
    [Display(Name = "MG")]
    MagicGladiator,

    /// <summary>Dark Lord, Lord Emperor.</summary>
    [Display(Name = "DL")]
    DarkLord,
}

/// <summary>
/// Where a PvP hit happens, for the PvP balance.
/// </summary>
public enum GkzPvpPlace
{
    /// <summary>Anywhere else.</summary>
    Normal,

    /// <summary>In a duel.</summary>
    Duel,

    /// <summary>In Chaos Castle.</summary>
    ChaosCastle,

    /// <summary>In Valley of Loren, the map of the castle siege.</summary>
    CastleSiege,
}

/// <summary>
/// The configuration of the PvP balance of MU GKZ. All values are percentages of the calculated damage.
/// The damage of a PvP hit is multiplied by the general percentage, the one of the place and the one of the
/// pair of attacker and victim class.
/// </summary>
public class GkzPvpBalanceConfiguration
{
    /// <summary>
    /// Gets or sets the percentage which applies to all PvP hits.
    /// </summary>
    [Display(Name = "Daño PvP general (%)", Description = "Se aplica a todos los golpes entre jugadores. 100 = sin cambios.")]
    public int GeneralPercent { get; set; } = 70;

    /// <summary>
    /// Gets or sets the percentage on normal maps.
    /// </summary>
    [Display(Name = "Mapas normales (%)")]
    public int NormalPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage in duels.
    /// </summary>
    [Display(Name = "Duelo (%)")]
    public int DuelPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage in Chaos Castle (it already halves the PvP damage on its own).
    /// </summary>
    [Display(Name = "Chaos Castle (%)", Description = "Chaos Castle ya reduce el daño PvP por su cuenta; esto se suma encima.")]
    public int ChaosCastlePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage in Valley of Loren, the map of the castle siege.
    /// </summary>
    [Display(Name = "Castle Siege / Valley of Loren (%)")]
    public int CastleSiegePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentages by attacker and victim class.
    /// </summary>
    [Display(Name = "Por clase: atacante contra víctima (%)", Description = "Ejemplo: Caballero contra Mago 80 = el DK le hace 80 % de daño al mago.")]
    public List<GkzPvpClassPair> ClassPairs { get; set; } = CreateDefaultClassPairs();

    /// <summary>
    /// Creates a pair for every combination of attacker and victim class, all at 100 %.
    /// </summary>
    /// <returns>The pairs.</returns>
    public static List<GkzPvpClassPair> CreateDefaultClassPairs() =>
        Enum.GetValues<GkzPvpClass>()
            .SelectMany(attacker => Enum.GetValues<GkzPvpClass>().Select(victim => new GkzPvpClassPair { Attacker = attacker, Victim = victim }))
            .ToList();

    /// <summary>
    /// Gets the factor by which the damage of a PvP hit is multiplied.
    /// </summary>
    /// <param name="attacker">The class of the attacker.</param>
    /// <param name="victim">The class of the victim.</param>
    /// <param name="place">The place of the hit.</param>
    /// <returns>The factor, e.g. 0.7 for 70 %.</returns>
    public double GetFactor(GkzPvpClass attacker, GkzPvpClass victim, GkzPvpPlace place)
    {
        var placePercent = place switch
        {
            GkzPvpPlace.Duel => this.DuelPercent,
            GkzPvpPlace.ChaosCastle => this.ChaosCastlePercent,
            GkzPvpPlace.CastleSiege => this.CastleSiegePercent,
            _ => this.NormalPercent,
        };

        var pairPercent = this.ClassPairs.FirstOrDefault(p => p.Attacker == attacker && p.Victim == victim)?.Percent ?? 100;
        return Math.Max(0, this.GeneralPercent) / 100.0 * Math.Max(0, placePercent) / 100.0 * Math.Max(0, pairPercent) / 100.0;
    }
}

/// <summary>
/// The percentage of the damage of an attacker class to a victim class.
/// </summary>
public class GkzPvpClassPair
{
    /// <summary>
    /// Gets or sets the class which attacks.
    /// </summary>
    [Display(Name = "Atacante")]
    public GkzPvpClass Attacker { get; set; }

    /// <summary>
    /// Gets or sets the class which gets hit.
    /// </summary>
    [Display(Name = "Víctima")]
    public GkzPvpClass Victim { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the damage.
    /// </summary>
    [Display(Name = "Daño (%)")]
    public int Percent { get; set; } = 100;

    /// <inheritdoc />
    public override string ToString() => $"{this.Attacker} → {this.Victim}: {this.Percent} %";
}
