// <copyright file="LongerItemDurabilityPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update makes the equipped items last three times longer.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("27855DE2-5408-4874-BE60-B233821D752C")]
public class LongerItemDurabilityPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: longer item durability";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Weapons, armors, wings, rings, pendants and pets lose their durability three times slower.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GkzItemDurability.Apply(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
