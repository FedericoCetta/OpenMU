// <copyright file="SeasonThreeScope.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Gens;
using MUnique.OpenMU.GameLogic.MiniGames.Doppelganger;
using MUnique.OpenMU.GameLogic.MiniGames.ImperialGuardian;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.Raklion;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

/// <summary>
/// Defines which content of the Season 6 data is <b>not</b> part of the MU GKZ Season 3 data.
/// Everything which isn't listed here stays as it's defined for Season 6.
/// </summary>
internal static class SeasonThreeScope
{
    /// <summary>
    /// The quest group of the quests which were available before the quest system of the Elf Soldier (Season 4+).
    /// </summary>
    internal const short LegacyQuestGroup = 0;

    /// <summary>
    /// The character classes which don't exist in Season 3: Summoner and Rage Fighter with their evolutions.
    /// </summary>
    internal static readonly IReadOnlySet<byte> RemovedCharacterClasses = new HashSet<byte>
    {
        (byte)CharacterClassNumber.Summoner,
        (byte)CharacterClassNumber.BloodySummoner,
        (byte)CharacterClassNumber.DimensionMaster,
        (byte)CharacterClassNumber.RageFighter,
        (byte)CharacterClassNumber.FistMaster,
    };

    /// <summary>
    /// The initializers of the maps which were introduced after Season 3, or belong to removed features.
    /// </summary>
    internal static readonly IReadOnlySet<Type> RemovedMapInitializerTypes = new HashSet<Type>
    {
        typeof(Elvenland),
        typeof(SwampOfCalmness),
        typeof(Raklion),
        typeof(RaklionBoss),
        typeof(Vulcanus),
        typeof(Doppelgaenger1),
        typeof(Doppelgaenger2),
        typeof(Doppelgaenger3),
        typeof(Doppelgaenger4),
        typeof(FortressOfImperialGuardian1),
        typeof(FortressOfImperialGuardian2),
        typeof(FortressOfImperialGuardian3),
        typeof(FortressOfImperialGuardian4),
        typeof(Karutan1),
        typeof(Karutan2),
        typeof(LorenMarket),
        typeof(SantaVillage),
    };

    /// <summary>
    /// The numbers of the maps of <see cref="RemovedMapInitializerTypes"/>.
    /// </summary>
    internal static readonly IReadOnlySet<short> RemovedMaps = new HashSet<short>
    {
        Elvenland.Number,
        SwampOfCalmness.Number,
        Raklion.Number,
        RaklionBoss.Number,
        Vulcanus.Number,
        Doppelgaenger1.Number,
        Doppelgaenger2.Number,
        Doppelgaenger3.Number,
        Doppelgaenger4.Number,
        FortressOfImperialGuardian1.Number,
        FortressOfImperialGuardian2.Number,
        FortressOfImperialGuardian3.Number,
        FortressOfImperialGuardian4.Number,
        Karutan1.Number,
        Karutan2.Number,
        LorenMarket.Number,
        SantaVillage.Number,
    };

    /// <summary>
    /// The NPCs whose spawns are removed from the remaining maps, because their feature doesn't exist in Season 3.
    /// </summary>
    internal static readonly IReadOnlySet<short> RemovedNpcSpawns = new HashSet<short>
    {
        543, // Gens Duprian
        544, // Gens Vanert
        547, // Market Union Member Julia (Loren Market)
        566, // Mercenary Guild Felicia
        568, // Wandering Merchant Zyro
        478, // Delgado (Lucky Coins)
        540, // Lugard (Doppelganger)
        541, // Compensation Box (Doppelganger)
        542, // Golden Compensation Box (Doppelganger)
        522, // Adviser Jerinteu (Imperial Guardian)
        450, // Cherry Blossom Spirit
        451, // Cherry Blossom Tree
    };

    /// <summary>
    /// The items which are removed explicitly. Items which are only usable by removed character classes
    /// and socket items are removed by rule, see <see cref="SeasonThreeContentPruner"/>.
    /// </summary>
    internal static readonly IReadOnlySet<(byte Group, short Number)> RemovedItems = CreateRemovedItems();

    /// <summary>
    /// The plugins which are deactivated, because their feature doesn't exist in Season 3.
    /// </summary>
    internal static readonly IReadOnlySet<Type> DeactivatedPlugIns = new HashSet<Type>
    {
        typeof(GensFeaturePlugIn),
        typeof(GensBattleZonePlugIn),
        typeof(GensMembershipPlugIn),
        typeof(GensRankingPlugIn),
        typeof(GensRelationshipRulesPlugIn),
        typeof(DoppelgangerFeaturePlugIn),
        typeof(DoppelgangerRewardChestPlugIn),
        typeof(ImperialGuardianFeaturePlugIn),
        typeof(RaklionPlugIn),
        typeof(UnlockSummonerAtLevel1),
        typeof(UnlockRageFighterAtLevel150),
        typeof(SummonerCharacterCardDeliveryPlugIn),
        typeof(RageFighterCharacterCardDeliveryPlugIn),
    };

    private static HashSet<(byte Group, short Number)> CreateRemovedItems()
    {
        var items = new HashSet<(byte Group, short Number)>();
        void AddRange(byte group, short from, short to)
        {
            for (var number = from; number <= to; number++)
            {
                items.Add((group, number));
            }
        }

        AddRange(12, 60, 65); // Seeds
        AddRange(12, 70, 74); // Spheres
        AddRange(12, 100, 129); // Seed spheres
        AddRange(12, 130, 135); // Small wings (Season 6)
        AddRange(14, 102, 109); // Imperial Guardian: Gaion's Order, Secromicon
        items.Add((13, 125)); // Doppelganger: Free Ticket
        items.Add((14, 101)); // Doppelganger: Suspicious Scrap of Paper
        items.Add((14, 110)); // Doppelganger: Sign of Dimensions
        items.Add((14, 111)); // Doppelganger: Mirror of Dimensions
        items.Add((14, 91)); // Summoner Character Card
        items.Add((14, 169)); // Rage Fighter Character Card
        return items;
    }
}
