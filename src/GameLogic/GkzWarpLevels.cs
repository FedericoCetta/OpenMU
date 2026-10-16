// <copyright file="GkzWarpLevels.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// The level which Magic Gladiators and Dark Lords need for warps and gates of MU GKZ: 80 % of the level requirement
/// (instead of the usual 2/3), and an own level on some of them. The client (NewUIMoveCommandWindow.cpp of MuMain)
/// shows the same levels.
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
            : GetLevel(character, warpInfo.LevelRequirement);

    /// <summary>
    /// Gets the level which the character needs for the gate.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="enterGate">The gate.</param>
    /// <returns>The level.</returns>
    public static int GetGateLevelRequirement(this Character character, EnterGate enterGate)
        => character.IsSpecialCharacter() && GateLevels.TryGetValue(enterGate.Number, out var level)
            ? level
            : GetLevel(character, enterGate.LevelRequirement);

    /// <summary>
    /// Gets the level for a level requirement: 80 % of it for Magic Gladiators and Dark Lords, except 400.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="levelRequirement">The level requirement.</param>
    /// <returns>The level.</returns>
    private static int GetLevel(Character character, int levelRequirement)
        => character.IsSpecialCharacter() && levelRequirement != 400 ? levelRequirement * 80 / 100 : levelRequirement;
}
