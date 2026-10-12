// <copyright file="AcceptOnlyGkzClientPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update makes the MU GKZ client the only accepted game client: the original client, its connect
/// server and its game server endpoints are removed, and the MU GKZ client gets its own serial.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2D274ED8-2F62-47E1-9B9C-54FDDF28600A")]
public class AcceptOnlyGkzClientPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: accept only the MU GKZ client";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Removes the original game client with its connect server and game server endpoints, so only the MU GKZ client can connect, and sets the serial of the MU GKZ client. Its connect server moves to port 44405. Requires a restart, and the MU GKZ client with the new serial.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var clients = (await context.GetAsync<GameClientDefinition>().ConfigureAwait(false)).ToList();
        if (clients.FirstOrDefault(c => c.Season == GkzClient.Season) is not { } gkzClient)
        {
            return;
        }

        GkzClient.Apply(gkzClient);

        var gameServers = (await context.GetAsync<GameServerDefinition>().ConfigureAwait(false)).ToList();
        var chatServers = (await context.GetAsync<ChatServerDefinition>().ConfigureAwait(false)).ToList();
        var connectServers = (await context.GetAsync<ConnectServerDefinition>().ConfigureAwait(false)).ToList();
        foreach (var otherClient in clients.Where(c => c != gkzClient))
        {
            foreach (var server in gameServers)
            {
                foreach (var endpoint in server.Endpoints.Where(e => e.Client == otherClient).ToList())
                {
                    server.Endpoints.Remove(endpoint);
                    await context.DeleteAsync(endpoint).ConfigureAwait(false);
                }
            }

            foreach (var endpoint in chatServers.SelectMany(s => s.Endpoints).Where(e => e.Client == otherClient))
            {
                endpoint.Client = gkzClient;
            }

            foreach (var connectServer in connectServers.Where(c => c.Client == otherClient).ToList())
            {
                connectServers.Remove(connectServer);
                await context.DeleteAsync(connectServer).ConfigureAwait(false);
            }

            await context.DeleteAsync(otherClient).ConfigureAwait(false);
        }

        foreach (var connectServer in connectServers.Where(c => c.Client == gkzClient))
        {
            connectServer.ClientListenerPort = GkzClient.ConnectServerPort;
            connectServer.Description = $"Connect Server ({GkzClient.Description})";
        }
    }
}
