// <copyright file="JewelBankConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>
/// The configuration of the jewel bank of MU GKZ: which items it holds, in the order the client shows them.
/// </summary>
public class JewelBankConfiguration
{
    /// <summary>
    /// The most items the bank can hold; the list must fit into one packet.
    /// </summary>
    public const int MaximumEntries = 30;

    /// <summary>
    /// Gets or sets the items which the bank holds. Items whose definition doesn't exist are left out.
    /// </summary>
    [Display(Name = "Items", Description = "The items which the jewel bank holds, by group, number and level, in the order the client shows them (30 at most).")]
    public List<JewelBankEntry> Items { get; set; } =
    [
        new(14, 13, 0), // Jewel of Bless
        new(14, 14, 0), // Jewel of Soul
        new(12, 15, 0), // Jewel of Chaos
        new(14, 16, 0), // Jewel of Life
        new(14, 22, 0), // Jewel of Creation
        new(14, 31, 0), // Jewel of Guardian
        new(14, 41, 0), // Gemstone
        new(14, 42, 0), // Jewel of Harmony
        new(14, 43, 0), // Lower Refine Stone
        new(14, 44, 0), // Higher Refine Stone
        new(13, 14, 0), // Loch's Feather
        new(13, 14, 1), // Crest of Monarch
        new(14, 96, 0), // Talisman of Chaos Assembly
        new(14, 53, 0), // Talisman of Luck
        new(14, 11, 0), // Box of Luck
        new(14, 11, 3), // Heart of Love
        new(14, 11, 8), // Box of Kundun +1
        new(14, 11, 9), // Box of Kundun +2
        new(14, 11, 10), // Box of Kundun +3
        new(14, 11, 11), // Box of Kundun +4
        new(14, 11, 12), // Box of Kundun +5
    ];
}
