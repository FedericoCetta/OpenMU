// <copyright file="GkzWarpLevels.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// The level which Magic Gladiators and Dark Lords need for some warps and gates of MU GKZ, instead of the usual
/// 2/3 of the level requirement. The client (NewUIMoveCommandWindow.cpp of MuMain) shows the same levels.
/// </summary>
public static class GkzWarpLevels
{
    /// <summary>
    /// The levels by warp index (WarpInfo.Index): Arena (150 for the other classes) and Crywolf (220).
    /// </summary>
    private static readonly Dictionary<int, int> WarpLevels = new() { [1] = 120, [24] = 200 };

    /// <summary>
    /// The levels by enter gate number: the gate from Valley of Loren to Crywolf.
    /// </summary>
    private static readonly Dictionary<int, int> GateLevels = new() { [117] = 200 };

    /// <summary>
    /// Gets the level which the character needs for the warp.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="warpInfo">The warp.</param>
    /// <returns>The level.</returns>
    public static int GetWarpLevelRequirement(this Character character, WarpInfo warpInfo)
        => character.IsSpecialCharacter() && WarpLevels.TryGetValue(warpInfo.Index, out var level)
            ? level
            : character.GetEffectiveMoveLevelRequirement(warpInfo.LevelRequirement);

    /// <summary>
    /// Gets the level which the character needs for the gate.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="enterGate">The gate.</param>
    /// <returns>The level.</returns>
    public static int GetGateLevelRequirement(this Character character, EnterGate enterGate)
        => character.IsSpecialCharacter() && GateLevels.TryGetValue(enterGate.Number, out var level)
            ? level
            : character.GetEffectiveMoveLevelRequirement(enterGate.LevelRequirement);
}
