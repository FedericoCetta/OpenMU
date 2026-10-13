// <copyright file="MarketListing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// MU GKZ: an item for sale in a <see cref="MarketStore"/>. The item is kept in its own storage while it's listed.
/// </summary>
public class MarketListing
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the storage which holds the listed item.
    /// </summary>
    [MemberOfAggregate]
    public virtual ItemStorage? Storage { get; set; }

    /// <summary>
    /// Gets or sets the price, in the <see cref="Currency"/>.
    /// </summary>
    public long Price { get; set; }

    /// <summary>
    /// Gets or sets the currency of the price.
    /// </summary>
    public MarketCurrency Currency { get; set; }

    /// <summary>
    /// Gets or sets the time when the listing was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the time when the listing ends; afterwards, the owner gets the item back.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Market listing for {this.Price} {this.Currency}";
    }
}
