// <copyright file="GkzChaosAssemblyTalisman.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

/// <summary>
/// The Talisman of Chaos Assembly of MU GKZ: put into the chaos machine with an upgrade to +11, +12 or +13,
/// a failure takes the item back to +0 instead of destroying it. The upgrades to +14 and +15 refuse it.
/// </summary>
internal static class GkzChaosAssemblyTalisman
{
    /// <summary>
    /// The levels of the upgrades which accept the talisman.
    /// </summary>
    internal static readonly IReadOnlySet<int> ProtectedTargetLevels = new HashSet<int> { 11, 12, 13 };

    /// <summary>
    /// Adds the talisman and uses it in the item level upgrades of the chaos machine.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        var talisman = gameConfiguration.Items.FirstOrDefault(IsTalisman) ?? CreateTalisman(context, gameConfiguration);
        var chaosMachine = gameConfiguration.Monsters.FirstOrDefault(m => m.NpcWindow == NpcWindow.ChaosMachine);
        foreach (var crafting in chaosMachine?.ItemCraftings ?? [])
        {
            if (GetUpgradeTargetLevel(crafting) is not { } targetLevel)
            {
                continue;
            }

            crafting.ItemCraftingHandlerClassName = typeof(GkzItemUpgradeCrafting).FullName!;
            var settings = crafting.SimpleCraftingSettings!;
            if (ProtectedTargetLevels.Contains(targetLevel) && !settings.RequiredItems.Any(r => r.PossibleItems.Any(IsTalisman)))
            {
                var requirement = context.CreateNew<ItemCraftingRequiredItem>();
                requirement.MinimumAmount = 0;
                requirement.MaximumAmount = 1;
                requirement.SuccessResult = MixResult.Disappear;
                requirement.FailResult = MixResult.Disappear;
                requirement.PossibleItems.Add(talisman);
                settings.RequiredItems.Add(requirement);
            }
        }
    }

    /// <summary>
    /// Gets the level which an item level upgrade crafting (+10 to +15) reaches.
    /// </summary>
    /// <param name="crafting">The crafting.</param>
    /// <returns>The level, or <c>null</c> if it's no item level upgrade.</returns>
    internal static int? GetUpgradeTargetLevel(ItemCrafting crafting)
    {
        if (crafting.SimpleCraftingSettings is not { } settings
            || !settings.ResultItems.Any(r => r.Reference == 1 && r.AddLevel == 1)
            || settings.RequiredItems.FirstOrDefault(r => r.Reference == 1) is not { } upgraded
            || upgraded.MinimumItemLevel != upgraded.MaximumItemLevel
            || upgraded.MinimumItemLevel is < 9 or > 14)
        {
            return null;
        }

        return upgraded.MinimumItemLevel + 1;
    }

    private static bool IsTalisman(ItemDefinition definition)
    {
        return definition.Group == GkzItemUpgradeCrafting.TalismanGroup && definition.Number == GkzItemUpgradeCrafting.TalismanNumber;
    }

    private static ItemDefinition CreateTalisman(IContext context, GameConfiguration gameConfiguration)
    {
        var talisman = context.CreateNew<ItemDefinition>();
        talisman.Name = "Talisman of Chaos Assembly";
        talisman.Group = GkzItemUpgradeCrafting.TalismanGroup;
        talisman.Number = GkzItemUpgradeCrafting.TalismanNumber;
        talisman.DropsFromMonsters = false;
        talisman.Durability = 1;
        talisman.Width = 1;
        talisman.Height = 1;
        talisman.SetGuid(talisman.Group, talisman.Number);
        gameConfiguration.Items.Add(talisman);
        return talisman;
    }
}
