// <copyright file="LorenciaBattleCenter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

/// <summary>
/// The battle center of MU GKZ: the plaza around the fountain of Lorencia is no safezone anymore,
/// so players can fight there with the normal player kill rules. The fountain is removed, and the
/// spawn point of Lorencia, which was on the plaza, is moved to the street west of it.
/// </summary>
/// <remarks>
/// The terrain changes have to match the terrain attributes of the client (Data\World1\EncTerrain1.att),
/// otherwise the client still prevents attacks in the safezone or walking through the fountain.
/// </remarks>
internal static class LorenciaBattleCenter
{
    /// <summary>
    /// The area of the battle center. Its walkable coordinates lose the safezone flag.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) Area = (132, 118, 151, 137);

    /// <summary>
    /// The coordinates which were blocked by the fountain. They become walkable.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) Fountain = (139, 126, 142, 129);

    /// <summary>
    /// The original spawn gate of Lorencia, which is on the plaza.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) OriginalSpawnGate = (133, 118, 151, 135);

    /// <summary>
    /// The new spawn gate of Lorencia: the street west of the plaza, which stays a safezone.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) SpawnGate = (113, 117, 119, 139);

    private const short LorenciaNumber = 0;

    private const int TerrainHeaderSize = 3;

    private const int TerrainSize = 256;

    private const byte Safezone = 1;

    private const byte Walkable = 0;

    /// <summary>
    /// Applies the battle center to the game configuration. It's idempotent.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Maps.FirstOrDefault(m => m.Number == LorenciaNumber) is not { } lorencia)
        {
            return;
        }

        UpdateTerrain(lorencia);
        MoveSpawnGate(lorencia);
    }

    /// <summary>
    /// Determines whether the given coordinate is inside of the specified area.
    /// </summary>
    /// <param name="area">The area.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns><c>true</c>, if the coordinate is inside of the area.</returns>
    internal static bool Contains((byte X1, byte Y1, byte X2, byte Y2) area, int x, int y)
    {
        return x >= area.X1 && x <= area.X2 && y >= area.Y1 && y <= area.Y2;
    }

    private static void UpdateTerrain(GameMapDefinition lorencia)
    {
        if (lorencia.TerrainData is not { Length: TerrainHeaderSize + (TerrainSize * TerrainSize) } terrain)
        {
            return;
        }

        // A new array, so that the change of the property is detected.
        var updated = (byte[])terrain.Clone();
        for (var y = Area.Y1; y <= Area.Y2; y++)
        {
            for (var x = Area.X1; x <= Area.X2; x++)
            {
                var index = TerrainHeaderSize + x + (y * TerrainSize);
                if (Contains(Fountain, x, y) || updated[index] == Safezone)
                {
                    // Other blocked coordinates keep their flags; the client even checks one of them.
                    updated[index] = Walkable;
                }
            }
        }

        if (!updated.AsSpan().SequenceEqual(terrain))
        {
            lorencia.TerrainData = updated;
        }
    }

    private static void MoveSpawnGate(GameMapDefinition lorencia)
    {
        var gate = lorencia.ExitGates.FirstOrDefault(g => g.IsSpawnGate
            && (g.X1, g.Y1, g.X2, g.Y2) == OriginalSpawnGate);
        if (gate is null)
        {
            return;
        }

        (gate.X1, gate.Y1, gate.X2, gate.Y2) = SpawnGate;
    }
}
