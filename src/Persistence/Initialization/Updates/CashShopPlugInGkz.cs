// <copyright file="CashShopPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the item shop of MU GKZ (X key), or brings it up to date with GkzCashShop.json.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E2A84C19-6F3B-4D57-9C21-8B0E5A7D3F42")]
public class CashShopPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: item shop";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "The item shop of the X key, paid with GKZ Coins: Talisman of Chaos Assembly, event invitations and transformation rings. Apply it again after a change of the catalog.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 13, 23, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GkzChaosAssemblyTalisman.Apply(context, gameConfiguration);
        GkzCashShop.Apply(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
