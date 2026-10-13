// <copyright file="GkzItemUpgradeCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// The item level upgrade (+10 to +15) of MU GKZ, with the Talisman of Chaos Assembly: when the upgrade fails,
/// the item goes back to +0 instead of disappearing. The talisman is only allowed where the crafting lists it
/// as an optional item (+11 to +13); in the other upgrades it's refused, so +14 and +15 stay pure chance.
/// </summary>
public class GkzItemUpgradeCrafting : SimpleItemCraftingHandler
{
    /// <summary>
    /// The item group of the Talisman of Chaos Assembly.
    /// </summary>
    public const byte TalismanGroup = 14;

    /// <summary>
    /// The item number of the Talisman of Chaos Assembly.
    /// </summary>
    public const short TalismanNumber = 96;

    private readonly SimpleCraftingSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzItemUpgradeCrafting"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public GkzItemUpgradeCrafting(SimpleCraftingSettings settings)
        : base(settings)
    {
        this._settings = settings;
    }

    /// <inheritdoc />
    public override CraftingResult? TryGetRequiredItems(Player player, out IList<CraftingRequiredItemLink> items, out byte successRate)
    {
        var talismanAllowed = this._settings.RequiredItems.Any(r => r.PossibleItems.Any(IsTalisman));
        if (!talismanAllowed && player.TemporaryStorage?.Items.Any(i => i.Definition is { } d && IsTalisman(d)) == true)
        {
            items = [];
            successRate = 0;
            _ = player.ShowBlueMessageAsync("El Talisman of Chaos Assembly solo sirve para subir a +11, +12 y +13.").AsTask();
            return CraftingResult.IncorrectMixItems;
        }

        if (base.TryGetRequiredItems(player, out items, out successRate) is { } error)
        {
            return error;
        }

        if (items.Any(link => link.ItemRequirement.PossibleItems.Any(IsTalisman) && link.Items.Any()))
        {
            // With the talisman, a failure takes the upgraded item back to +0 instead of destroying it.
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].ItemRequirement is { Reference: > 0, FailResult: MixResult.Disappear } requirement)
                {
                    items[i] = new CraftingRequiredItemLink(items[i].Items, CopyWithFailResult(requirement, MixResult.DowngradedToZero));
                }
            }
        }

        return default;
    }

    private static bool IsTalisman(ItemDefinition definition)
    {
        return definition.Group == TalismanGroup && definition.Number == TalismanNumber;
    }

    private static TransientItemCraftingRequiredItem CopyWithFailResult(ItemCraftingRequiredItem requirement, MixResult failResult)
    {
        var copy = new TransientItemCraftingRequiredItem
        {
            Reference = requirement.Reference,
            MinimumAmount = requirement.MinimumAmount,
            MaximumAmount = requirement.MaximumAmount,
            MinimumItemLevel = requirement.MinimumItemLevel,
            MaximumItemLevel = requirement.MaximumItemLevel,
            SuccessResult = requirement.SuccessResult,
            FailResult = failResult,
            NpcPriceDivisor = requirement.NpcPriceDivisor,
            AddPercentage = requirement.AddPercentage,
        };

        foreach (var item in requirement.PossibleItems)
        {
            copy.PossibleItems.Add(item);
        }

        foreach (var option in requirement.RequiredItemOptions)
        {
            copy.RequiredItemOptions.Add(option);
        }

        return copy;
    }
}
