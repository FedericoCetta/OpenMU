// <copyright file="LimitConnectionsPerIpPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update limits the simultaneous logged in accounts per IP address to 2, on all game servers together.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5BB0AE76-01A8-488A-B514-925B2326C87C")]
public class LimitConnectionsPerIpPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: 2 accounts per IP";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Activates the limit of simultaneous logged in accounts per IP address with a maximum of 2, counted on all game servers together. Applies to new logins.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 12, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var typeId = typeof(MaximumConnectionsPerIpPlugIn).GUID;
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeId) is not { } plugInConfiguration)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        GkzConnectionLimit.Apply(plugInConfiguration);
        return ValueTask.CompletedTask;
    }
}
