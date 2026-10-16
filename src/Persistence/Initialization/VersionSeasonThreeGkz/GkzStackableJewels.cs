// <copyright file="GkzStackableJewels.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The jewels of MU GKZ stack up to <see cref="MaximumStack"/> pieces in one inventory slot.
/// </summary>
/// <remarks>
/// For an item which isn't wearable, the durability of its definition is the size of a stack, and the durability
/// of the item is the number of pieces. Using a jewel on an item takes one piece of the stack.
/// The MU GKZ client must know the same items and size (CNewUIInventoryCtrl::AreItemsStackable).
/// </remarks>
internal static class GkzStackableJewels
{
    /// <summary>
    /// The most jewels in one stack.
    /// </summary>
    internal const byte MaximumStack = 50;

    /// <summary>
    /// The most boxes (Box of Luck, Box of Kundun) in one stack.
    /// </summary>
    internal const byte MaximumBoxStack = 64;

    /// <summary>
    /// Gets the jewels, by group and number.
    /// </summary>
    internal static IReadOnlyList<(byte Group, short Number)> Jewels { get; } =
    [
        (14, 13), // Jewel of Bless
        (14, 14), // Jewel of Soul
        (14, 16), // Jewel of Life
        (12, 15), // Jewel of Chaos
        (14, 22), // Jewel of Creation
        (14, 31), // Jewel of Guardian
        (14, 41), // Gemstone
        (14, 42), // Jewel of Harmony
        (14, 43), // Lower Refine Stone
        (14, 44), // Higher Refine Stone
        (14, 11), // Box of Luck, also the Box of Kundun +1 to +5 (they stack only with the same level)
    ];

    /// <summary>
    /// Makes the jewels of the configuration stackable.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The number of changed item definitions.</returns>
    internal static int Apply(GameConfiguration gameConfiguration)
    {
        var changed = 0;
        foreach (var definition in gameConfiguration.Items.Where(IsJewel))
        {
            var maximum = definition is { Group: 14, Number: 11 } ? MaximumBoxStack : MaximumStack;
            if (definition.Durability != maximum)
            {
                definition.Durability = maximum;
                changed++;
            }
        }

        return changed;
    }

    private static bool IsJewel(ItemDefinition definition)
    {
        return definition.ItemSlot is null && Jewels.Contains((definition.Group, definition.Number));
    }
}
