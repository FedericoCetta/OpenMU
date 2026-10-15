// <copyright file="GkzCrywolfWarp.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The warp to Crywolf Fortress of the move list (index 24, like the original game), which the Season 6 data lacks.
/// The third class quests start there: Priest Devin, and the Werewolf Quarrel leads into the Barracks of Balgass.
/// </summary>
internal static class GkzCrywolfWarp
{
    /// <summary>
    /// The index of the warp; the move list of the client (Movereq.bmd) uses the same.
    /// </summary>
    internal const ushort WarpIndex = 24;

    private const short CrywolfMapNumber = 34;

    /// <summary>
    /// Adds the warp, if it's missing.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.WarpList.Any(w => w.Index == WarpIndex))
        {
            return;
        }

        // The spawn gate where the players arrive from the Valley of Loren.
        var gate = gameConfiguration.Maps.FirstOrDefault(m => m.Number == CrywolfMapNumber)?.ExitGates
            .FirstOrDefault(g => g is { IsSpawnGate: true, X1: 229, Y1: 37 });
        if (gate is null)
        {
            return;
        }

        var warp = context.CreateNew<WarpInfo>();
        warp.Index = WarpIndex;
        warp.Name = "Crywolf";
        warp.Costs = 15000;
        warp.LevelRequirement = 10;
        warp.Gate = gate;
        gameConfiguration.WarpList.Add(warp);
    }
}
