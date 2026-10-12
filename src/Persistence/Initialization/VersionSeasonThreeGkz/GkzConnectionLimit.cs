// <copyright file="GkzConnectionLimit.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The limit of simultaneous logged in accounts per IP address of MU GKZ, against excessive multi accounting.
/// </summary>
internal static class GkzConnectionLimit
{
    /// <summary>
    /// The maximum number of accounts which can be logged in at the same time from one IP address, on all game servers together.
    /// </summary>
    internal const int MaximumConnectionsPerIp = 2;

    /// <summary>
    /// Activates and configures the <see cref="MaximumConnectionsPerIpPlugIn"/>.
    /// </summary>
    /// <param name="plugInConfiguration">The configuration of the plugin.</param>
    internal static void Apply(PlugInConfiguration plugInConfiguration)
    {
        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(
            new MaximumConnectionsPerIpPlugInConfiguration
            {
                MaximumConnectionsPerIp = MaximumConnectionsPerIp,
                CountAllGameServers = true,
            },
            null);
    }
}
