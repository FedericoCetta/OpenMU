// <copyright file="GkzResetSetDeliveryPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Delivers the set of the reward of 501 resets: all pieces of the chosen set at +13 +28, with luck, three
/// excellent options and, if the set has it, the option of level 380. The armor is the item of the product.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("3B7E91D4-5A2C-4F16-B8E0-6D14C9A7F352")]
public class GkzResetSetDeliveryPlugIn : ICashShopProductDeliveryPlugIn
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: set del premio de 501 resets";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Entrega todas las piezas del set elegido, +13 +28 con Luck y 3 opciones Exc.";

    private const byte SetItemLevel = 13;
    private const byte ArmorGroup = 8;
    private const byte FirstPieceGroup = 7;
    private const byte LastPieceGroup = 11;

    // Reflect, damage decrease and maximum health: the options which count in PvP. Never all six.
    private static readonly int[] ExcellentOptionNumbers = [3, 4, 6];

    // The strategy is one instance for all players; the pieces are kept until they're shown.
    private readonly ConditionalWeakTable<Player, List<Item>> _addedItems = new();

    /// <inheritdoc />
    public ItemIdentifier Key => new(null, ArmorGroup);

    /// <inheritdoc />
    public bool CanDeliver(CashShopProduct product) => product.Duration == TimeSpan.Zero;

    /// <summary>
    /// Gets the pieces of the set of the armor: the items of the armor groups with the number of the armor.
    /// </summary>
    /// <param name="items">The items of the game.</param>
    /// <param name="armor">The armor of the set.</param>
    /// <returns>The pieces; some classes have no helm.</returns>
    public static List<ItemDefinition> GetPieces(IEnumerable<ItemDefinition> items, ItemDefinition armor)
    {
        return items.Where(i => i.Group is >= FirstPieceGroup and <= LastPieceGroup && i.Number == armor.Number).OrderBy(i => i.Group).ToList();
    }

    /// <summary>
    /// Adds the options of the reward to a piece of the set.
    /// </summary>
    /// <param name="item">The piece, with its definition.</param>
    /// <param name="createLink">Creates a link to an option.</param>
    public static void ApplyOptions(Item item, Func<ItemOptionLink> createLink)
    {
        item.Level = SetItemLevel;
        item.Durability = item.GetMaximumDurabilityOfOnePiece();

        var options = item.Definition!.PossibleItemOptions.SelectMany(o => o.PossibleOptions).ToList();
        void Add(IncreasableItemOption option, int level = 0)
        {
            var link = createLink();
            link.ItemOption = option;
            link.Level = level;
            item.ItemOptions.Add(link);
        }

        if (options.FirstOrDefault(o => o.OptionType == ItemOptionTypes.Option) is { } normalOption)
        {
            Add(normalOption, normalOption.LevelDependentOptions.Select(l => l.Level).DefaultIfEmpty(0).Max());
        }

        foreach (var option in options.Where(o => o.OptionType == ItemOptionTypes.Luck
                                                  || o.OptionType == ItemOptionTypes.GuardianOption
                                                  || (o.OptionType == ItemOptionTypes.Excellent && ExcellentOptionNumbers.Contains(o.Number))))
        {
            Add(option);
        }
    }

    /// <inheritdoc />
    public async ValueTask<CashShopUseResult> DeliverAsync(Player player, CashShopProduct product)
    {
        if (player.Inventory is not { } inventory || product.ItemDefinition is not { } armor)
        {
            return CashShopUseResult.CannotUse;
        }

        var addedItems = new List<Item>();
        foreach (var definition in GetPieces(player.GameContext.Configuration.Items, armor))
        {
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            ApplyOptions(item, () => player.PersistenceContext.CreateNew<ItemOptionLink>());
            if (!await inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                foreach (var addedItem in addedItems)
                {
                    await inventory.RemoveItemAsync(addedItem).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(addedItem).ConfigureAwait(false);
                }

                return CashShopUseResult.InventoryFull;
            }

            addedItems.Add(item);
        }

        this._addedItems.AddOrUpdate(player, addedItems);
        return CashShopUseResult.Success;
    }

    /// <inheritdoc />
    public async ValueTask DeliveredAsync(Player player, CashShopProduct product)
    {
        if (!this._addedItems.TryGetValue(player, out var addedItems))
        {
            return;
        }

        this._addedItems.Remove(player);
        foreach (var item in addedItems)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        }
    }
}
