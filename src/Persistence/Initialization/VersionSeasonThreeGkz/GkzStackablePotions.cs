// <copyright file="GkzStackablePotions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The potions of MU GKZ stack up to <see cref="MaximumStack"/> pieces in one inventory slot.
/// </summary>
/// <remarks>
/// Like <see cref="GkzStackableJewels"/>: the durability of the definition is the size of a stack, and the
/// durability of the item is the number of pieces. Merchants can sell stacks up to this size.
/// The MU GKZ client must know the same items and size (CNewUIInventoryCtrl::AreItemsStackable).
/// The siege potion keeps its own stack.
/// </remarks>
internal static class GkzStackablePotions
{
    /// <summary>
    /// The most potions in one stack.
    /// </summary>
    internal const byte MaximumStack = 255;

    /// <summary>
    /// Gets the potions, by group and number.
    /// </summary>
    internal static IReadOnlyList<(byte Group, short Number)> Potions { get; } =
    [
        (14, 0), // Apple
        (14, 1), // Small Healing Potion
        (14, 2), // Medium Healing Potion
        (14, 3), // Large Healing Potion
        (14, 4), // Small Mana Potion
        (14, 5), // Medium Mana Potion
        (14, 6), // Large Mana Potion
        (14, 8), // Antidote
        (14, 35), // Small Shield Potion
        (14, 36), // Medium Shield Potion
        (14, 37), // Large Shield Potion
        (14, 38), // Small Complex Potion
        (14, 39), // Medium Complex Potion
        (14, 40), // Large Complex Potion
    ];

    /// <summary>
    /// Makes the potions of the configuration stackable.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The number of changed item definitions.</returns>
    internal static int Apply(GameConfiguration gameConfiguration)
    {
        var changed = 0;
        foreach (var definition in gameConfiguration.Items.Where(IsPotion))
        {
            if (definition.Durability != MaximumStack)
            {
                definition.Durability = MaximumStack;
                changed++;
            }
        }

        return changed;
    }

    private static bool IsPotion(ItemDefinition definition)
    {
        return definition.ItemSlot is null && Potions.Contains((definition.Group, definition.Number));
    }
}
