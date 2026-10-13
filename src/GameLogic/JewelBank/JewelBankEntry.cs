// <copyright file="JewelBankEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>
/// An item which the jewel bank holds.
/// </summary>
public class JewelBankEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JewelBankEntry"/> class.
    /// </summary>
    public JewelBankEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JewelBankEntry"/> class.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <param name="level">The item level.</param>
    public JewelBankEntry(byte group, short number, byte level)
    {
        this.Group = group;
        this.Number = number;
        this.Level = level;
    }

    /// <summary>
    /// Gets or sets the group of the item definition.
    /// </summary>
    public byte Group { get; set; }

    /// <summary>
    /// Gets or sets the number of the item definition.
    /// </summary>
    public short Number { get; set; }

    /// <summary>
    /// Gets or sets the level of the item, e.g. 8 to 12 for the Box of Kundun +1 to +5.
    /// </summary>
    public byte Level { get; set; }

    /// <summary>
    /// Determines whether the item is of this entry.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if the item is of this kind.</returns>
    public bool Matches(Item item)
    {
        return item.Definition is { } definition
               && definition.Group == this.Group
               && definition.Number == this.Number
               && item.Level == this.Level;
    }

    /// <inheritdoc />
    public override string ToString() => $"{this.Group}/{this.Number} +{this.Level}";
}
