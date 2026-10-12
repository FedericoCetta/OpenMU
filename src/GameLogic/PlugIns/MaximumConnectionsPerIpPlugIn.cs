// <copyright file="MaximumConnectionsPerIpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin that limits the number of simultaneous active player sessions connected from the same IP address.
/// </summary>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Name), Description = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Description), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
[Guid("2C779F5E-379E-4CE0-BFCC-CA6455D757B3")]
public class MaximumConnectionsPerIpPlugIn : IPlayerStateChangingPlugIn, ISupportCustomConfiguration<MaximumConnectionsPerIpPlugInConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>
    /// The game servers of this process, so that the limit can apply to all of them together.
    /// Weak references, so stopped game servers don't stay in memory.
    /// </summary>
    private static readonly ConditionalWeakTable<IGameContext, object> GameContexts = new();

    /// <inheritdoc />
    public MaximumConnectionsPerIpPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangingAsync(Player player, StateMachine.StateChangeEventArgs eventArgs)
    {
        if (eventArgs.NextState != PlayerState.Authenticated)
        {
            return;
        }

        var ipAddress = (player as IHasIpAddress)?.IpAddress;
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        var config = this.Configuration ?? CreateDefaultConfiguration();
        GameContexts.TryAdd(player.GameContext, new object());
        var contexts = config.CountAllGameServers
            ? GameContexts.Select(entry => entry.Key).ToList()
            : [player.GameContext];
        var players = new List<Player>();
        foreach (var context in contexts)
        {
            players.AddRange(await context.GetPlayersAsync().ConfigureAwait(false));
        }

        var sameIpCount = players.Count(p =>
            (p as IHasIpAddress)?.IpAddress == ipAddress
            && p != player
            && p.PlayerState.CurrentState != PlayerState.Initial
            && p.PlayerState.CurrentState != PlayerState.LoginScreen);

        if (sameIpCount >= config.MaximumConnectionsPerIp)
        {
            player.Logger.LogWarning(
                "Login request for IP '{IpAddress}' was cancelled. It exceeded the maximum connection limit of {Limit}.",
                ipAddress,
                config.MaximumConnectionsPerIp);
            player.LoginResultOverride = Views.Login.LoginResult.ServerIsFull;
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        return CreateDefaultConfiguration();
    }

    private static MaximumConnectionsPerIpPlugInConfiguration CreateDefaultConfiguration()
    {
        return new MaximumConnectionsPerIpPlugInConfiguration();
    }
}
