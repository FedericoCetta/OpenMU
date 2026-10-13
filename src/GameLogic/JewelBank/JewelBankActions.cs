// <copyright file="JewelBankActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.JewelBank;

/// <summary>
/// The actions of the jewel bank: show it, deposit all of an item from the inventory, withdraw some, and the
/// automatic saving of picked up items.
/// </summary>
public class JewelBankActions
{
    /// <summary>
    /// Shows the bank.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask ShowAsync(Player player)
    {
        if (JewelBankPlugIn.GetConfiguration(player.GameContext) is not { } configuration || player.Account is not { } account)
        {
            return;
        }

        var items = GetEntries(player, configuration)
            .Select(entry => (entry, GetBankItem(account, entry)?.Count ?? 0))
            .ToList();
        await player.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(p => p.ShowJewelBankAsync(items, account.AutoSaveToBank)).ConfigureAwait(false);
    }

    /// <summary>
    /// Moves all pieces of an item from the inventory into the bank.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <param name="level">The item level.</param>
    public async ValueTask DepositAsync(Player player, byte group, short number, byte level)
    {
        if (FindEntry(player, group, number, level) is not { } entry || player.Account is not { } account || player.Inventory is not { } inventory)
        {
            return;
        }

        var items = inventory.Items.Where(i => IsInInventory(i) && IsPlain(i) && entry.Matches(i)).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var pieces = 0;
        foreach (var item in items)
        {
            var slot = item.ItemSlot;
            pieces += Pieces(item);
            await inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
        }

        GetOrCreateBankItem(player, account, entry).Count += pieces;
        await SaveAsync(player).ConfigureAwait(false);
        player.Logger.LogInformation("Player {player} put {pieces} x {item} into the jewel bank.", player, pieces, entry);
        await this.ShowAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Moves pieces of an item from the bank into the inventory, as far as there's room.
    /// Stackable items fill the stacks in the inventory first, then new stacks.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <param name="level">The item level.</param>
    /// <param name="amount">The number of pieces.</param>
    public async ValueTask WithdrawAsync(Player player, byte group, short number, byte level, int amount)
    {
        if (FindEntry(player, group, number, level) is not { } entry
            || player.Account is not { } account
            || player.Inventory is not { } inventory
            || GetBankItem(account, entry) is not { Count: > 0 } bankItem
            || player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == group && d.Number == number) is not { } definition)
        {
            return;
        }

        var remaining = Math.Min(amount, bankItem.Count);
        var maximumStack = definition.ItemSlot is null && definition.Durability > 1 ? definition.Durability : 1;
        if (maximumStack > 1)
        {
            foreach (var stack in inventory.Items.Where(i => IsInInventory(i) && IsPlain(i) && entry.Matches(i) && i.Durability < maximumStack).ToList())
            {
                var added = Math.Min(remaining, maximumStack - (int)stack.Durability);
                stack.Durability += added;
                remaining -= added;
                bankItem.Count -= added;
                await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(stack, false)).ConfigureAwait(false);
                if (remaining == 0)
                {
                    break;
                }
            }
        }

        while (remaining > 0)
        {
            var pieces = Math.Min(remaining, maximumStack);
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Level = level;
            item.Durability = maximumStack > 1 ? pieces : item.GetMaximumDurabilityOfOnePiece();
            if (!await inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                await player.ShowBlueMessageAsync("No hay más lugar en el inventario.").ConfigureAwait(false);
                break;
            }

            remaining -= pieces;
            bankItem.Count -= pieces;
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        }

        await SaveAsync(player).ConfigureAwait(false);
        await this.ShowAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets whether picked up items go straight into the bank.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="autoSave">Whether to save picked up items automatically.</param>
    public async ValueTask SetAutoSaveAsync(Player player, bool autoSave)
    {
        if (player.Account is not { } account || JewelBankPlugIn.GetConfiguration(player.GameContext) is null)
        {
            return;
        }

        account.AutoSaveToBank = autoSave;
        await SaveAsync(player).ConfigureAwait(false);
        await this.ShowAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts a picked up item into the bank, if the account saves automatically and the bank holds the item.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="droppedItem">The dropped item.</param>
    /// <returns><c>true</c>, if the item went into the bank.</returns>
    public async ValueTask<bool> TryAutoSaveAsync(Player player, DroppedItem droppedItem)
    {
        if (player.Account is not { AutoSaveToBank: true } account
            || JewelBankPlugIn.GetConfiguration(player.GameContext) is not { } configuration
            || !IsPlain(droppedItem.Item)
            || configuration.Items.FirstOrDefault(e => e.Matches(droppedItem.Item)) is not { } entry)
        {
            return false;
        }

        var pieces = Pieces(droppedItem.Item);
        if (!await droppedItem.TryPickUpIntoBankAsync(player, () => GetOrCreateBankItem(player, account, entry).Count += pieces).ConfigureAwait(false))
        {
            return false;
        }

        await player.ShowBlueMessageAsync($"{droppedItem.Item.Definition!.Name.ValueInNeutralLanguage}{(entry.Level > 0 ? $" +{entry.Level}" : string.Empty)} guardado en el banco.").ConfigureAwait(false);
        return true;
    }

    private static IEnumerable<JewelBankEntry> GetEntries(Player player, JewelBankConfiguration configuration)
    {
        var items = player.GameContext.Configuration.Items;
        return configuration.Items
            .Where(e => items.Any(d => d.Group == e.Group && d.Number == e.Number && e.Level <= d.MaximumItemLevel))
            .Take(JewelBankConfiguration.MaximumEntries);
    }

    private static JewelBankEntry? FindEntry(Player player, byte group, short number, byte level)
    {
        return JewelBankPlugIn.GetConfiguration(player.GameContext) is { } configuration
            ? GetEntries(player, configuration).FirstOrDefault(e => e.Group == group && e.Number == number && e.Level == level)
            : null;
    }

    private static AccountBankItem? GetBankItem(Account account, JewelBankEntry entry)
    {
        return account.BankItems.FirstOrDefault(b => b.ItemGroup == entry.Group && b.ItemNumber == entry.Number && b.ItemLevel == entry.Level);
    }

    private static AccountBankItem GetOrCreateBankItem(Player player, Account account, JewelBankEntry entry)
    {
        if (GetBankItem(account, entry) is { } existing)
        {
            return existing;
        }

        var bankItem = player.PersistenceContext.CreateNew<AccountBankItem>();
        bankItem.ItemGroup = entry.Group;
        bankItem.ItemNumber = entry.Number;
        bankItem.ItemLevel = entry.Level;
        account.BankItems.Add(bankItem);
        return bankItem;
    }

    private static bool IsInInventory(Item item)
    {
        return item.ItemSlot >= InventoryConstants.EquippableSlotsCount && item.ItemSlot < InventoryConstants.FirstStoreItemSlotIndex;
    }

    /// <summary>
    /// Only items without options go into the bank, because it just counts them.
    /// </summary>
    private static bool IsPlain(Item item)
    {
        return item.ItemOptions.Count == 0 && !item.HasSkill && item.ItemSetGroups.Count == 0;
    }

    private static int Pieces(Item item)
    {
        return item.IsStackable() ? Math.Max(1, (int)item.Durability) : 1;
    }

    private static async ValueTask SaveAsync(Player player)
    {
        try
        {
            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                player.Logger.LogWarning("Couldn't save the jewel bank of player {player} now; it's saved later.", player);
            }
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Couldn't save the jewel bank of player {player} now; it's saved later.", player);
        }
    }
}
