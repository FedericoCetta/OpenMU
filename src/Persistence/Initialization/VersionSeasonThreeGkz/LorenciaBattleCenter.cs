// <copyright file="LorenciaBattleCenter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using System.IO;

/// <summary>
/// The battle center of MU GKZ: the fountain of Lorencia is replaced by a round PvP ring.
/// Inside of the ring, players can fight with the normal player kill rules.
/// The spawn point of Lorencia, which covered the plaza, is limited to the north part of the plaza.
/// </summary>
/// <remarks>
/// The terrain changes have to match the terrain attributes of the client (Data\World1\EncTerrain1.att),
/// otherwise the client still prevents attacks in the safezone or walking through the fountain.
/// The ring model itself only exists in the client (see tools\gkz_lorencia_battle_center.py).
/// </remarks>
internal static class LorenciaBattleCenter
{
    /// <summary>
    /// The radius of the fighting area inside of the ring, in coordinates.
    /// </summary>
    internal const double RingRadius = 4.5;

    private const short LorenciaNumber = 0;

    private const int TerrainHeaderSize = 3;

    private const int TerrainSize = 256;

    private const byte Safezone = 1;

    private const byte Walkable = 0;

    /// <summary>
    /// The center of the ring, in coordinates. It's the center of the removed fountain.
    /// </summary>
    internal static readonly (double X, double Y) RingCenter = (141.0, 128.0);

    /// <summary>
    /// The coordinates which were blocked by the fountain. They become walkable.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) Fountain = (139, 126, 142, 129);

    /// <summary>
    /// The original spawn gate of Lorencia, which covers the whole plaza.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) OriginalSpawnGate = (133, 118, 151, 135);

    /// <summary>
    /// The spawn gate of the first version of the battle center, on the street west of the plaza.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) FirstVersionSpawnGate = (113, 117, 119, 139);

    /// <summary>
    /// The spawn gate of Lorencia: the north part of the plaza, which keeps a distance to the ring.
    /// </summary>
    internal static readonly (byte X1, byte Y1, byte X2, byte Y2) SpawnGate = (133, 118, 151, 122);

    /// <summary>
    /// Applies the battle center to the game configuration. It's idempotent, and the terrain of
    /// Lorencia is always derived from its original, so earlier versions of the battle center are replaced.
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
    /// Determines whether the given coordinate is inside of the ring, i.e. where players can fight.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns><c>true</c>, if the coordinate is inside of the ring.</returns>
    internal static bool IsInRing(int x, int y)
    {
        // The coordinate covers the area from (x, y) to (x + 1, y + 1), so its center is used.
        var dx = x + 0.5 - RingCenter.X;
        var dy = y + 0.5 - RingCenter.Y;
        return (dx * dx) + (dy * dy) <= RingRadius * RingRadius;
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
        if (LoadOriginalTerrain() is not { Length: TerrainHeaderSize + (TerrainSize * TerrainSize) } updated)
        {
            return;
        }

        for (var y = 0; y < TerrainSize; y++)
        {
            for (var x = 0; x < TerrainSize; x++)
            {
                var index = TerrainHeaderSize + x + (y * TerrainSize);
                if (Contains(Fountain, x, y) || (IsInRing(x, y) && updated[index] == Safezone))
                {
                    // Other blocked coordinates keep their flags; the client even checks one of them.
                    updated[index] = Walkable;
                }
            }
        }

        if (lorencia.TerrainData is null || !updated.AsSpan().SequenceEqual(lorencia.TerrainData))
        {
            lorencia.TerrainData = updated;
        }
    }

    private static byte[]? LoadOriginalTerrain()
    {
        var assembly = typeof(LorenciaBattleCenter).Assembly;
        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.Terrain1.att");
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static void MoveSpawnGate(GameMapDefinition lorencia)
    {
        var gate = lorencia.ExitGates.FirstOrDefault(g => g.IsSpawnGate
            && ((g.X1, g.Y1, g.X2, g.Y2) == OriginalSpawnGate || (g.X1, g.Y1, g.X2, g.Y2) == FirstVersionSpawnGate));
        if (gate is null)
        {
            return;
        }

        (gate.X1, gate.Y1, gate.X2, gate.Y2) = SpawnGate;
    }
}
