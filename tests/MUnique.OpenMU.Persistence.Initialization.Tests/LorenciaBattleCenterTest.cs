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
/// Tests the PvP ring of MU GKZ, which replaces the fountain of Lorencia.
/// </summary>
[TestFixture]
internal class LorenciaBattleCenterTest
{
    private const int TerrainHeaderSize = 3;

    /// <summary>
    /// Tests that a new database contains the ring.
    /// </summary>
    [Test]
    public async Task NewDatabaseContainsRingAsync()
    {
        var (_, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);

        AssertRing(configuration);
    }

    /// <summary>
    /// Tests that the update adds the ring to a database with the original Lorencia, and that applying it twice changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateAddsRingToOriginalLorenciaAsync()
    {
        var (provider, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        var lorencia = GetLorencia(configuration);
        lorencia.UpdateTerrainFromResources();
        SetSpawnGate(lorencia, LorenciaBattleCenter.OriginalSpawnGate);

        await ApplyTwiceAndAssertAsync(provider, configuration).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the update replaces the first version of the battle center, which covered the whole plaza.
    /// </summary>
    [Test]
    public async Task UpdateReplacesFirstVersionAsync()
    {
        var (provider, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        var lorencia = GetLorencia(configuration);
        lorencia.UpdateTerrainFromResources();
        var terrain = (byte[])lorencia.TerrainData!.Clone();
        for (var y = 118; y <= 137; y++)
        {
            for (var x = 132; x <= 151; x++)
            {
                if (terrain[TerrainHeaderSize + x + (y * 256)] == 1)
                {
                    terrain[TerrainHeaderSize + x + (y * 256)] = 0;
                }
            }
        }

        lorencia.TerrainData = terrain;
        SetSpawnGate(lorencia, LorenciaBattleCenter.FirstVersionSpawnGate);

        await ApplyTwiceAndAssertAsync(provider, configuration).ConfigureAwait(false);
    }

    private static async Task ApplyTwiceAndAssertAsync(InMemoryPersistenceContextProvider provider, GameConfiguration configuration)
    {
        var lorencia = GetLorencia(configuration);
        var update = new AddLorenciaBattleCenterPlugInGkz();
        await update.ApplyUpdateAsync(provider.CreateNewContext(), configuration).ConfigureAwait(false);
        var terrainAfterFirstUpdate = lorencia.TerrainData;
        await update.ApplyUpdateAsync(provider.CreateNewContext(), configuration).ConfigureAwait(false);

        AssertRing(configuration);
        Assert.That(lorencia.TerrainData, Is.SameAs(terrainAfterFirstUpdate), "the second update changed the terrain");
    }

    private static void AssertRing(GameConfiguration configuration)
    {
        var lorencia = GetLorencia(configuration);
        var original = new GameMapDefinition { Number = lorencia.Number, Name = lorencia.Name };
        original.UpdateTerrainFromResources();

        Assert.Multiple(() =>
        {
            for (var y = 110; y <= 145; y++)
            {
                for (var x = 125; x <= 158; x++)
                {
                    var value = GetValue(lorencia, x, y);
                    var originalValue = GetValue(original, x, y);
                    if (LorenciaBattleCenter.Contains(LorenciaBattleCenter.Fountain, x, y))
                    {
                        Assert.That(value, Is.Zero, $"({x}, {y}) of the fountain isn't walkable");
                    }
                    else if (LorenciaBattleCenter.IsInRing(x, y) && originalValue == 1)
                    {
                        Assert.That(value, Is.Zero, $"({x}, {y}) in the ring is still a safezone");
                    }
                    else
                    {
                        Assert.That(value, Is.EqualTo(originalValue), $"({x}, {y}) outside of the ring was changed");
                    }
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
                    Assert.That(LorenciaBattleCenter.IsInRing(x, y), Is.False, $"spawn ({x}, {y}) is inside of the ring");
                }
            }
        });
    }

    private static void SetSpawnGate(GameMapDefinition lorencia, (byte X1, byte Y1, byte X2, byte Y2) area)
    {
        var spawnGate = lorencia.ExitGates.Single(g => g.IsSpawnGate);
        (spawnGate.X1, spawnGate.Y1, spawnGate.X2, spawnGate.Y2) = area;
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
