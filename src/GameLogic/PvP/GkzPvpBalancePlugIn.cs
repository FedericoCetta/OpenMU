// <copyright file="GkzPvpBalancePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The PvP balance of MU GKZ: percentages of the damage between players, in general, by place and by the classes
/// of attacker and victim. It's edited in the admin panel and applies without a restart.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: balance PvP", Description = "Porcentajes del daño entre jugadores: general, por lugar y por clase atacante contra víctima.")]
[Guid("5B8E2F4A-1C7D-4E93-A6B0-3D9F8C2E7A14")]
public class GkzPvpBalancePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<GkzPvpBalanceConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The number of Valley of Loren, the map of the castle siege.
    /// </summary>
    private const short CastleSiegeMapNumber = 30;

    /// <inheritdoc />
    public GkzPvpBalanceConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the base class of a character class.
    /// </summary>
    /// <param name="classNumber">The number of the character class.</param>
    /// <returns>The base class.</returns>
    public static GkzPvpClass GetClass(byte classNumber) => classNumber switch
    {
        <= 3 => GkzPvpClass.DarkWizard,
        <= 7 => GkzPvpClass.DarkKnight,
        <= 11 => GkzPvpClass.Elf,
        <= 13 => GkzPvpClass.MagicGladiator,
        _ => GkzPvpClass.DarkLord,
    };

    /// <summary>
    /// Applies the PvP balance to the damage of a hit of a player to another player.
    /// </summary>
    /// <param name="damage">The calculated damage.</param>
    /// <param name="attacker">The attacking player (the owner, for a hit of the raven).</param>
    /// <param name="victim">The player who gets hit.</param>
    /// <returns>The damage after the balance; at least 1 when it was positive.</returns>
    public static int Apply(int damage, Player attacker, Player victim)
    {
        if (damage <= 0
            || attacker.GameContext.FeaturePlugIns.GetPlugIn<GkzPvpBalancePlugIn>() is not { Configuration: { } configuration }
            || attacker.SelectedCharacter?.CharacterClass is not { } attackerClass
            || victim.SelectedCharacter?.CharacterClass is not { } victimClass)
        {
            return damage;
        }

        var place = attacker.DuelRoom?.Opponent == victim ? GkzPvpPlace.Duel
            : attacker.CurrentMiniGame?.Definition.Type == MiniGameType.ChaosCastle ? GkzPvpPlace.ChaosCastle
            : attacker.CurrentMap?.Definition.Number == CastleSiegeMapNumber ? GkzPvpPlace.CastleSiege
            : GkzPvpPlace.Normal;
        var factor = configuration.GetFactor(GetClass(attackerClass.Number), GetClass(victimClass.Number), place);
        return Math.Max(1, (int)(damage * factor));
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GkzPvpBalanceConfiguration();
}
