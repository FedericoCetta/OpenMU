// <copyright file="LorenciaBattleCenterTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the battle center of MU GKZ on the plaza of Lorencia.
/// </summary>
[TestFixture]
internal class LorenciaBattleCenterTest
{
    private const int TerrainHeaderSize = 3;

    /// <summary>
    /// Tests that a new database contains the battle center.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsBattleCenterAsync()
    {
        var (_, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);

        AssertBattleCenter(configuration);
    }

    /// <summary>
    /// Tests that the update adds the battle center to an existing database, and that applying it twice changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateAddsBattleCenterAsync()
    {
        var (provider, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        var lorencia = GetLorencia(configuration);
        lorencia.UpdateTerrainFromResources();
        var spawnGate = lorencia.ExitGates.Single(g => g.IsSpawnGate);
        (spawnGate.X1, spawnGate.Y1, spawnGate.X2, spawnGate.Y2) = LorenciaBattleCenter.OriginalSpawnGate;
        Assert.That(GetValue(lorencia, 140, 127), Is.Not.Zero, "precondition: the fountain is blocked");

        var update = new AddLorenciaBattleCenterPlugInGkz();
        await update.ApplyUpdateAsync(provider.CreateNewContext(), configuration).ConfigureAwait(false);
        var terrainAfterFirstUpdate = lorencia.TerrainData;
        await update.ApplyUpdateAsync(provider.CreateNewContext(), configuration).ConfigureAwait(false);

        AssertBattleCenter(configuration);
        Assert.That(lorencia.TerrainData, Is.SameAs(terrainAfterFirstUpdate), "the second update changed the terrain");
    }

    private static void AssertBattleCenter(GameConfiguration configuration)
    {
        var lorencia = GetLorencia(configuration);
        var area = LorenciaBattleCenter.Area;
        Assert.Multiple(() =>
        {
            for (var y = area.Y1; y <= area.Y2; y++)
            {
                for (var x = area.X1; x <= area.X2; x++)
                {
                    Assert.That(GetValue(lorencia, x, y), Is.Not.EqualTo(1), $"({x}, {y}) is still a safezone");
                }
            }

            for (var y = LorenciaBattleCenter.Fountain.Y1; y <= LorenciaBattleCenter.Fountain.Y2; y++)
            {
                for (var x = LorenciaBattleCenter.Fountain.X1; x <= LorenciaBattleCenter.Fountain.X2; x++)
                {
                    Assert.That(GetValue(lorencia, x, y), Is.Zero, $"({x}, {y}) of the fountain isn't walkable");
                }
            }

            // The client checks this coordinate to validate the terrain of Lorencia.
            Assert.That(GetValue(lorencia, 135, 123), Is.EqualTo(5));

            var spawnGate = lorencia.ExitGates.Single(g => g.IsSpawnGate);
            Assert.That((spawnGate.X1, spawnGate.Y1, spawnGate.X2, spawnGate.Y2), Is.EqualTo(LorenciaBattleCenter.SpawnGate));
            for (var y = spawnGate.Y1; y <= spawnGate.Y2; y++)
            {
                for (var x = spawnGate.X1; x <= spawnGate.X2; x++)
                {
                    Assert.That(LorenciaBattleCenter.Contains(area, x, y), Is.False, $"spawn ({x}, {y}) is inside of the battle center");
                }
            }
        });
    }

    private static byte GetValue(GameMapDefinition map, int x, int y) => map.TerrainData![TerrainHeaderSize + x + (y * 256)];

    private static GameMapDefinition GetLorencia(GameConfiguration configuration) => configuration.Maps.Single(m => m.Number == 0);

    private static async Task<(InMemoryPersistenceContextProvider Provider, GameConfiguration Configuration)> CreateConfigurationAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var initialization = new DataInitialization(provider, NullLoggerFactory.Instance);
        await initialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        return (provider, (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First());
    }
}
