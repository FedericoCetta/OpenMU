// <copyright file="ItemCombinationAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Vip;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Vip;

/// <summary>
/// The item combination of MU GKZ: two equal excellent items of the same level become one new item of that
/// level, with 1 to 3 new random excellent options. It always gets luck, and keeps the skill if one of the two
/// had it; the additional option is random, as in a drop. It costs coins.
/// </summary>
public class ItemCombinationAction
{
    /// <summary>
    /// The most excellent options of a combined item.
    /// </summary>
    public const int MaximumExcellentOptions = 3;

    private readonly IRandomizer _randomizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCombinationAction"/> class.
    /// </summary>
    public ItemCombinationAction()
        : this(Rand.GetRandomizer())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCombinationAction"/> class.
    /// </summary>
    /// <param name="randomizer">The randomizer.</param>
    public ItemCombinationAction(IRandomizer randomizer)
    {
        this._randomizer = randomizer;
    }

    /// <summary>
    /// Checks whether the two items can be combined, and shows the confirmation with the price, or what's missing.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sourceSlot">The inventory slot of the dragged item.</param>
    /// <param name="targetSlot">The inventory slot of the item it was dropped on.</param>
    public async ValueTask CheckAsync(Player player, byte sourceSlot, byte targetSlot)
    {
        if (player.Account is { } && VipPlugIn.GetConfiguration(player.GameContext) is not null)
        {
            await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        }

        var status = this.GetStatus(player, sourceSlot, targetSlot, out var configuration);
        var coins = configuration is null ? 0 : player.Account!.GetCashShopCoins(configuration.CoinType);
        var price = configuration?.CombinationPrice ?? 0;
        await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowItemCombinationCheckAsync(status, sourceSlot, targetSlot, price, coins)).ConfigureAwait(false);
    }

    /// <summary>
    /// Combines the two items, after checking again.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sourceSlot">The inventory slot of the dragged item.</param>
    /// <param name="targetSlot">The inventory slot of the item it was dropped on; the new item goes there.</param>
    public async ValueTask CombineAsync(Player player, byte sourceSlot, byte targetSlot)
    {
        var result = await player.RunPersistenceExclusiveAsync(async () =>
        {
            await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
            var status = this.GetStatus(player, sourceSlot, targetSlot, out var configuration);
            if (status != ItemCombinationStatus.Ok)
            {
                return status;
            }

            return await this.CombineCheckedAsync(player, sourceSlot, targetSlot, configuration!).ConfigureAwait(false);
        }).ConfigureAwait(false);

        await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowItemCombinationResultAsync(result)).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the item has at least one excellent option, and can be combined.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if the item can be combined.</returns>
    internal static bool CanBeCombined(Item item)
    {
        return item.Definition is not null && item.IsExcellent() && !item.IsAncient();
    }

    /// <summary>
    /// Creates the combined item.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <param name="configuration">The configuration with the chances of the excellent options.</param>
    /// <param name="maximumOptionLevel">The highest level of the additional option, as for drops.</param>
    /// <param name="context">The context which creates the item.</param>
    /// <returns>The new item.</returns>
    internal Item CreateCombinedItem(Item first, Item second, VipConfiguration configuration, int maximumOptionLevel, Persistence.IContext context)
    {
        var definition = first.Definition!;
        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Level = first.Level;
        item.HasSkill = (first.HasSkill || second.HasSkill) && item.CanHaveSkill();

        // Always with luck: the combination should feel like an upgrade.
        if (FindOptionDefinition(definition, ItemOptionTypes.Luck) is { } luck)
        {
            this.AddOption(item, luck.PossibleOptions.First(), 0, context);
        }

        if (FindOptionDefinition(definition, ItemOptionTypes.Option) is { } additional && additional.AddsRandomly)
        {
            this.AddRandomAdditionalOption(item, additional, maximumOptionLevel, context);
        }

        if (FindOptionDefinition(definition, ItemOptionTypes.Excellent) is { } excellent)
        {
            var count = Math.Min(this.GetExcellentOptionCount(configuration), Math.Min(excellent.MaximumOptionsPerItem, excellent.PossibleOptions.Count));
            foreach (var option in excellent.PossibleOptions.OrderBy(_ => this._randomizer.NextDouble()).Take(Math.Max(1, count)))
            {
                this.AddOption(item, option, 0, context);
            }
        }

        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        return item;
    }

    private static ItemOptionDefinition? FindOptionDefinition(ItemDefinition definition, ItemOptionType optionType)
    {
        return definition.PossibleItemOptions.FirstOrDefault(o => o.PossibleOptions.Any(p => p.OptionType == optionType));
    }

    private static bool IsInventorySlot(byte slot)
    {
        return slot >= InventoryConstants.EquippableSlotsCount && slot < InventoryConstants.FirstStoreItemSlotIndex;
    }

    private ItemCombinationStatus GetStatus(Player player, byte sourceSlot, byte targetSlot, out VipConfiguration? configuration)
    {
        configuration = VipPlugIn.GetConfiguration(player.GameContext);
        if (configuration is null || player.Account is not { } account || player.Inventory is not { } inventory)
        {
            return ItemCombinationStatus.NotAvailable;
        }

        if (sourceSlot == targetSlot
            || !IsInventorySlot(sourceSlot)
            || !IsInventorySlot(targetSlot)
            || inventory.GetItem(sourceSlot) is not { } first
            || inventory.GetItem(targetSlot) is not { } second
            || !CanBeCombined(first)
            || !CanBeCombined(second)
            || first.Definition != second.Definition
            || first.Level != second.Level)
        {
            return ItemCombinationStatus.InvalidItems;
        }

        if (configuration.CombinationRequiresVip && !account.IsVip())
        {
            return ItemCombinationStatus.NotVip;
        }

        if (account.GetCashShopCoins(configuration.CoinType) < configuration.CombinationPrice)
        {
            return ItemCombinationStatus.NotEnoughCoins;
        }

        return ItemCombinationStatus.Ok;
    }

    private async ValueTask<ItemCombinationStatus> CombineCheckedAsync(Player player, byte sourceSlot, byte targetSlot, VipConfiguration configuration)
    {
        var account = player.Account!;
        var inventory = player.Inventory!;
        var first = inventory.GetItem(targetSlot)!;
        var second = inventory.GetItem(sourceSlot)!;
        var maximumOptionLevel = player.GameContext.Configuration.MaximumItemOptionLevelDrop is >= 1 and <= 4 and var level ? level : 3;
        var newItem = this.CreateCombinedItem(first, second, configuration, maximumOptionLevel, player.PersistenceContext);

        var coins = account.GetCashShopCoins(configuration.CoinType);
        account.SetCashShopCoins(configuration.CoinType, coins - configuration.CombinationPrice);
        await inventory.RemoveItemAsync(first).ConfigureAwait(false);
        await inventory.RemoveItemAsync(second).ConfigureAwait(false);
        await inventory.AddItemAsync(targetSlot, newItem).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(first).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(second).ConfigureAwait(false);

        try
        {
            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                // Like the cash shop: the combination stays done in the game, the next save stores it.
                player.Logger.LogWarning("Couldn't save the item combination of player {player} now; it's saved later.", player);
            }
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Couldn't save the item combination of player {player} now; it's saved later.", player);
        }

        player.Logger.LogInformation(
            "Player {player} combined two {item} +{level} into one with the options {options}, for {price} {coinType}.",
            player,
            newItem.Definition!.Name.ValueInNeutralLanguage,
            newItem.Level,
            string.Join(", ", newItem.ItemOptions.Select(o => $"{o.ItemOption?.OptionType?.Name} {o.ItemOption?.Number} (level {o.Level})")),
            configuration.CombinationPrice,
            configuration.CoinType);
        await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(sourceSlot)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(targetSlot)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(newItem)).ConfigureAwait(false);
        return ItemCombinationStatus.Ok;
    }

    private int GetExcellentOptionCount(VipConfiguration configuration)
    {
        var roll = this._randomizer.NextDouble();
        if (roll < configuration.CombinationThreeOptionsChance)
        {
            return MaximumExcellentOptions;
        }

        return roll < configuration.CombinationThreeOptionsChance + configuration.CombinationTwoOptionsChance ? 2 : 1;
    }

    private void AddRandomAdditionalOption(Item item, ItemOptionDefinition definition, int maximumLevel, Persistence.IContext context)
    {
        if (!this._randomizer.NextRandomBool(definition.AddChance) || definition.PossibleOptions.FirstOrDefault() is not { } option)
        {
            return;
        }

        var levels = option.LevelDependentOptions.Select(o => o.Level).Append(1).Distinct().Where(l => l <= maximumLevel).DefaultIfEmpty(1).ToList();
        this.AddOption(item, option, levels[this._randomizer.NextInt(0, levels.Count)], context);
    }

    private void AddOption(Item item, IncreasableItemOption option, int level, Persistence.IContext context)
    {
        var link = context.CreateNew<ItemOptionLink>();
        link.ItemOption = option;
        link.Level = level;
        item.ItemOptions.Add(link);
    }
}
