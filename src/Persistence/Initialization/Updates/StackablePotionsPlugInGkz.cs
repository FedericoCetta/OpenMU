// <copyright file="StackablePotionsPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update makes the potions stackable up to 255 pieces in one inventory slot.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8835E84C-0E63-49F1-A78C-752CC0F22A54")]
public class StackablePotionsPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: stackable potions";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Potions (healing, mana, shield, complex, apple, antidote) stack up to 255 pieces in one inventory slot.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GkzStackablePotions.Apply(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
