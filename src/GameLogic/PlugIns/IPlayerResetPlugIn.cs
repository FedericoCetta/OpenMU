// <copyright file="IPlayerResetPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a character did a reset (MU GKZ: the reset rewards).
/// </summary>
[Guid("3B7D2E94-5A1C-4F86-A0E3-9C4B6D1F2A57")]
[PlugInPoint("Player reset", "Is called when a character did a reset.")]
public interface IPlayerResetPlugIn
{
    /// <summary>
    /// Is called when the character of the player did a reset, before it's logged out.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="resetCount">The number of resets of the character, including this one.</param>
    /// <returns>The task.</returns>
    ValueTask PlayerResetAsync(Player player, int resetCount);
}
