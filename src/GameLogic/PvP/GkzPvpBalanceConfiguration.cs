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
/// The damage of a PvP hit is multiplied by the general percentage, the one of the place, the one of the attacker's
/// class and the one of the victim's class.
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
    /// Gets or sets the most damage which the victim's armor and wings take away in PvP, in percent.
    /// </summary>
    [Display(Name = "Reducción de daño máxima (%)", Description = "Lo máximo que sacan juntas las alas y las opciones de reducción de daño en PvP. Las alas de 3ª +15 solas sacan un 69 %.")]
    public int MaxDamageReductionPercent { get; set; } = 60;

    /// <summary>
    /// Gets or sets the most damage which the soul barrier takes away in PvP, in percent.
    /// </summary>
    [Display(Name = "Soul Barrier máximo (%)")]
    public int MaxSoulBarrierPercent { get; set; } = 60;

    /// <summary>
    /// Gets or sets the most damage which the elf's damage buff (Greater Damage) adds in PvP.
    /// </summary>
    [Display(Name = "Buff de daño de la Elfa máximo", Description = "Lo máximo que suma el buff de daño en PvP (con 32767 de energía daría unos 4700).")]
    public int MaxGreaterDamageBonus { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the most defense which the elf's defense buff (Greater Defense) adds in PvP.
    /// </summary>
    [Display(Name = "Buff de defensa de la Elfa máximo", Description = "Lo máximo que suma el buff de defensa en PvP (con 32767 de energía daría unos 4100).")]
    public int MaxGreaterDefenseBonus { get; set; } = 800;

    /// <summary>
    /// Gets or sets the most damage which is reflected to an attacking player, in percent.
    /// </summary>
    [Display(Name = "Reflejo de daño máximo (%)")]
    public int MaxReflectPercent { get; set; } = 70;

    /// <summary>
    /// Gets or sets the highest chance of a critical hit in PvP, in percent.
    /// </summary>
    [Display(Name = "Chance de crítico máxima (%)")]
    public int MaxCriticalChancePercent { get; set; } = 80;

    /// <summary>
    /// Gets or sets the highest chance of an excellent hit in PvP, in percent.
    /// </summary>
    [Display(Name = "Chance de excellent máxima (%)")]
    public int MaxExcellentChancePercent { get; set; } = 70;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkWizard does to any player.
    /// </summary>
    [Display(Name = "Mago ataca (%)", Description = "El daño que hace esta clase a cualquier jugador. 100 = sin cambios.")]
    public int DarkWizardAttackPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkKnight does to any player.
    /// </summary>
    [Display(Name = "Caballero ataca (%)", Description = "El daño que hace esta clase a cualquier jugador. 100 = sin cambios.")]
    public int DarkKnightAttackPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the Elf does to any player.
    /// </summary>
    [Display(Name = "Elfa ataca (%)", Description = "El daño que hace esta clase a cualquier jugador. 100 = sin cambios.")]
    public int ElfAttackPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the MagicGladiator does to any player.
    /// </summary>
    [Display(Name = "MG ataca (%)", Description = "El daño que hace esta clase a cualquier jugador. 100 = sin cambios.")]
    public int MagicGladiatorAttackPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkLord does to any player.
    /// </summary>
    [Display(Name = "DL ataca (%)", Description = "El daño que hace esta clase a cualquier jugador. 100 = sin cambios.")]
    public int DarkLordAttackPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkWizard receives from any player.
    /// </summary>
    [Display(Name = "Mago recibe (%)", Description = "El daño que recibe esta clase de cualquier jugador. 100 = sin cambios.")]
    public int DarkWizardReceivePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkKnight receives from any player.
    /// </summary>
    [Display(Name = "Caballero recibe (%)", Description = "El daño que recibe esta clase de cualquier jugador. 100 = sin cambios.")]
    public int DarkKnightReceivePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the Elf receives from any player.
    /// </summary>
    [Display(Name = "Elfa recibe (%)", Description = "El daño que recibe esta clase de cualquier jugador. 100 = sin cambios.")]
    public int ElfReceivePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the MagicGladiator receives from any player.
    /// </summary>
    [Display(Name = "MG recibe (%)", Description = "El daño que recibe esta clase de cualquier jugador. 100 = sin cambios.")]
    public int MagicGladiatorReceivePercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the percentage of the damage which the DarkLord receives from any player.
    /// </summary>
    [Display(Name = "DL recibe (%)", Description = "El daño que recibe esta clase de cualquier jugador. 100 = sin cambios.")]
    public int DarkLordReceivePercent { get; set; } = 100;

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

        return Math.Max(0, this.GeneralPercent) / 100.0 * Math.Max(0, placePercent) / 100.0
            * Math.Max(0, this.GetAttackPercent(attacker)) / 100.0 * Math.Max(0, this.GetReceivePercent(victim)) / 100.0;
    }

    /// <summary>
    /// Gets the percentage of the damage which the class does.
    /// </summary>
    /// <param name="attacker">The class of the attacker.</param>
    /// <returns>The percentage.</returns>
    public int GetAttackPercent(GkzPvpClass attacker) => attacker switch
    {
        GkzPvpClass.DarkWizard => this.DarkWizardAttackPercent,
        GkzPvpClass.DarkKnight => this.DarkKnightAttackPercent,
        GkzPvpClass.Elf => this.ElfAttackPercent,
        GkzPvpClass.MagicGladiator => this.MagicGladiatorAttackPercent,
        _ => this.DarkLordAttackPercent,
    };

    /// <summary>
    /// Gets the percentage of the damage which the class receives.
    /// </summary>
    /// <param name="victim">The class of the victim.</param>
    /// <returns>The percentage.</returns>
    public int GetReceivePercent(GkzPvpClass victim) => victim switch
    {
        GkzPvpClass.DarkWizard => this.DarkWizardReceivePercent,
        GkzPvpClass.DarkKnight => this.DarkKnightReceivePercent,
        GkzPvpClass.Elf => this.ElfReceivePercent,
        GkzPvpClass.MagicGladiator => this.MagicGladiatorReceivePercent,
        _ => this.DarkLordReceivePercent,
    };
}
