// <copyright file="AccountBankItem.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The number of pieces of one item in the jewel bank of an account (MU GKZ), e.g. 78 Jewels of Bless.
/// </summary>
/// <remarks>
/// The bank only holds items without options, like jewels, boxes and talismans, so the kind of item is
/// enough: its group, number and level (e.g. the Box of Kundun +3 is group 14, number 11, level 10).
/// </remarks>
public class AccountBankItem
{
    /// <summary>
    /// Gets or sets the group of the item definition.
    /// </summary>
    public byte ItemGroup { get; set; }

    /// <summary>
    /// Gets or sets the number of the item definition.
    /// </summary>
    public short ItemNumber { get; set; }

    /// <summary>
    /// Gets or sets the level of the item.
    /// </summary>
    public byte ItemLevel { get; set; }

    /// <summary>
    /// Gets or sets the number of pieces in the bank.
    /// </summary>
    public int Count { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.Count} x {this.ItemGroup}/{this.ItemNumber} +{this.ItemLevel}";
    }
}
