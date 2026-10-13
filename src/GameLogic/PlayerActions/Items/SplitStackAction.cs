// <copyright file="SplitStackAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// MU GKZ: takes one piece off a stack in the inventory (Ctrl + click) and puts it into a free slot.
/// </summary>
public class SplitStackAction
{
    /// <summary>
    /// Takes one piece off the stack at the slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The inventory slot of the stack.</param>
    public async ValueTask SplitAsync(Player player, byte slot)
    {
        if (player.Inventory is not { } inventory
            || slot < InventoryConstants.EquippableSlotsCount
            || slot >= InventoryConstants.FirstStoreItemSlotIndex
            || player.PlayerState.CurrentState == PlayerState.TradeOpened
            || player.PlayerState.CurrentState == PlayerState.TradeButtonPressed
            || inventory.GetItem(slot) is not { } stack
            || !stack.IsStackable()
            || stack.Durability < 2)
        {
            return;
        }

        var piece = player.PersistenceContext.CreateNew<Item>();
        piece.AssignValues(stack);
        piece.Durability = 1;
        if (!await inventory.AddItemAsync(piece).ConfigureAwait(false))
        {
            await player.PersistenceContext.DeleteAsync(piece).ConfigureAwait(false);
            await player.ShowBlueMessageAsync("No hay lugar en el inventario.").ConfigureAwait(false);
            return;
        }

        stack.Durability -= 1;
        await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(stack, false)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(piece)).ConfigureAwait(false);
    }
}
