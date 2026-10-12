// <copyright file="GkzClientTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests that only the MU GKZ client is accepted.
/// </summary>
[TestFixture]
internal class GkzClientTest
{
    /// <summary>
    /// Tests that a new database only knows the MU GKZ client, with its serial, on port 44405.
    /// </summary>
    [Test]
    public async Task NewDatabaseOnlyHasGkzClientAsync()
    {
        var provider = await CreateDatabaseAsync().ConfigureAwait(false);

        await AssertOnlyGkzClientAsync(provider).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that the update removes the original client from a database which still has it, and that it can run twice.
    /// </summary>
    [Test]
    public async Task UpdateRemovesOriginalClientAsync()
    {
        var provider = await CreateDatabaseAsync().ConfigureAwait(false);
        using (var context = provider.CreateNewContext())
        {
            // The state of a database before the update: the original client with its own listeners.
            var original = context.CreateNew<GameClientDefinition>();
            original.Season = 6;
            original.Episode = 3;
            original.Serial = "k1Pk2jcET48mxL3b"u8.ToArray();
            var connectServer = context.CreateNew<ConnectServerDefinition>();
            connectServer.Client = original;
            connectServer.ClientListenerPort = 44405;
            foreach (var server in await context.GetAsync<GameServerDefinition>().ConfigureAwait(false))
            {
                var endpoint = context.CreateNew<GameServerEndpoint>();
                endpoint.Client = original;
                endpoint.NetworkPort = 45950;
                server.Endpoints.Add(endpoint);
            }

            foreach (var endpoint in (await context.GetAsync<ChatServerDefinition>().ConfigureAwait(false)).SelectMany(s => s.Endpoints))
            {
                endpoint.Client = original;
            }

            var gkzConnectServer = (await context.GetAsync<ConnectServerDefinition>().ConfigureAwait(false)).First(c => c.Client!.Season == GkzClient.Season);
            gkzConnectServer.ClientListenerPort = 44406;
            (await context.GetAsync<GameClientDefinition>().ConfigureAwait(false)).First(c => c.Season == GkzClient.Season).Serial = "k1Pk2jcET48mxL3b"u8.ToArray();
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var update = new AcceptOnlyGkzClientPlugInGkz();
        for (var i = 0; i < 2; i++)
        {
            using var context = provider.CreateNewContext();
            var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        await AssertOnlyGkzClientAsync(provider).ConfigureAwait(false);
    }

    private static async Task AssertOnlyGkzClientAsync(InMemoryPersistenceContextProvider provider)
    {
        using var context = provider.CreateNewContext();
        var client = (await context.GetAsync<GameClientDefinition>().ConfigureAwait(false)).Single();
        Assert.That(client.Season, Is.EqualTo(GkzClient.Season));
        Assert.That(client.Serial, Is.EqualTo(GkzClient.Serial.ToArray()));

        var connectServer = (await context.GetAsync<ConnectServerDefinition>().ConfigureAwait(false)).Single();
        Assert.That(connectServer.ClientListenerPort, Is.EqualTo(GkzClient.ConnectServerPort));
        Assert.That(connectServer.Client, Is.SameAs(client));

        var endpoints = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).SelectMany(s => s.Endpoints).ToList();
        Assert.That(endpoints, Is.Not.Empty);
        Assert.That(endpoints.Select(e => e.Client), Has.All.SameAs(client));
        Assert.That((await context.GetAsync<ChatServerDefinition>().ConfigureAwait(false)).SelectMany(s => s.Endpoints).Select(e => e.Client), Has.All.SameAs(client));
    }

    private static async Task<InMemoryPersistenceContextProvider> CreateDatabaseAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new DataInitialization(provider, NullLoggerFactory.Instance).CreateInitialDataAsync(1, false).ConfigureAwait(false);
        return provider;
    }
}
