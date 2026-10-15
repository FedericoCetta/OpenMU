// <copyright file="IShowGkzGuardChallengePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.AntiCheat;

/// <summary>
/// Sends the challenge of the anti-cheat to the MU GKZ client, which starts its heartbeats with it.
/// </summary>
public interface IShowGkzGuardChallengePlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the challenge.
    /// </summary>
    /// <param name="challenge">The challenge.</param>
    ValueTask ShowChallengeAsync(uint challenge);
}
