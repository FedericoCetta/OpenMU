// <copyright file="GkzInvasionInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// A running invasion, for the invasion window of the MU GKZ client.
/// </summary>
/// <param name="Name">The name of the invasion.</param>
/// <param name="MapNumber">The number of the (first) announced map.</param>
/// <param name="SecondsLeft">The seconds until the invasion ends.</param>
/// <param name="Monsters">The monsters of the invasion.</param>
public record GkzInvasionInfo(string Name, ushort MapNumber, int SecondsLeft, IReadOnlyList<GkzInvasionMonsterInfo> Monsters);

/// <summary>
/// A kind of monster of a running invasion.
/// </summary>
/// <param name="MonsterNumber">The number of the monster definition.</param>
/// <param name="Left">How many are still alive.</param>
/// <param name="Total">How many were spawned.</param>
/// <param name="IsBoss">Whether it's the featured monster (the boss) of the invasion.</param>
public record GkzInvasionMonsterInfo(ushort MonsterNumber, int Left, int Total, bool IsBoss);

/// <summary>
/// An invasion which can tell its state to the invasion window of the MU GKZ client.
/// </summary>
public interface IGkzInvasionInfoProvider
{
    /// <summary>
    /// Gets the running invasion of the game context.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The invasion, or <c>null</c> when it doesn't run.</returns>
    GkzInvasionInfo? GetRunningInvasion(IGameContext gameContext);
}

/// <summary>
/// The running invasions of a game context.
/// </summary>
public static class GkzInvasions
{
    /// <summary>
    /// Gets the running invasions.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The running invasions.</returns>
    public static IReadOnlyList<GkzInvasionInfo> GetRunning(IGameContext gameContext)
    {
        return gameContext.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>()
            .OfType<IGkzInvasionInfoProvider>()
            .Select(p => p.GetRunningInvasion(gameContext))
            .OfType<GkzInvasionInfo>()
            .ToList();
    }
}
