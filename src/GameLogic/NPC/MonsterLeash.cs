// <copyright file="MonsterLeash.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// MU GKZ: keeps the random walk of a monster near its spawn area. Without it, every random step starts at the
/// current position, so the monsters drift away from their spot over time and stay wherever a chase ended.
/// </summary>
public static class MonsterLeash
{
    /// <summary>
    /// Gets the target of a random step, kept inside the spawn area extended by the margin. A monster which is
    /// outside of it (e.g. after chasing a player) walks back towards it.
    /// </summary>
    /// <param name="position">The current position.</param>
    /// <param name="moveByX">The random step in x.</param>
    /// <param name="moveByY">The random step in y.</param>
    /// <param name="area">The spawn area; <c>null</c> if the monster has none.</param>
    /// <param name="margin">How many cells the monster may walk outside of its spawn area.</param>
    /// <returns>The target of the step.</returns>
    public static Point GetRandomStepTarget(Point position, int moveByX, int moveByY, MonsterSpawnArea? area, int margin)
    {
        var x = position.X + moveByX;
        var y = position.Y + moveByY;
        if (area is not null)
        {
            x = Math.Clamp(x, Math.Min(area.X1, area.X2) - margin, Math.Max(area.X1, area.X2) + margin);
            y = Math.Clamp(y, Math.Min(area.Y1, area.Y2) - margin, Math.Max(area.Y1, area.Y2) + margin);
        }

        return new Point((byte)Math.Clamp(x, 0, 0xFF), (byte)Math.Clamp(y, 0, 0xFF));
    }
}
