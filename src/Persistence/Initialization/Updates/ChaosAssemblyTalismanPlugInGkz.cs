// <copyright file="ChaosAssemblyTalismanPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the Talisman of Chaos Assembly: a failed upgrade to +11, +12 or +13 takes the item back to +0
/// instead of destroying it.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5B9E3C27-8D41-4A6F-B2E0-1C7D9F4A8E63")]
public class ChaosAssemblyTalismanPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: Talisman of Chaos Assembly";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds the Talisman of Chaos Assembly: with it, a failed upgrade to +11, +12 or +13 takes the item back to +0 instead of destroying it. +14 and +15 refuse it.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 13, 22, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GkzChaosAssemblyTalisman.Apply(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
