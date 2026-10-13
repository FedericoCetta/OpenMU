// <copyright file="MarketStore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// MU GKZ: the store of an account in the market, where its items are for sale, even while it's offline.
/// The store is open while it's paid (<see cref="PaidUntil"/>); the money of the sold items waits here until
/// the owner collects it.
/// </summary>
[AggregateRoot]
public class MarketStore
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the account which owns the store.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the name of the character which opened the store, shown as seller.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time until which the store is paid and open.
    /// </summary>
    public DateTime PaidUntil { get; set; }

    /// <summary>
    /// Gets or sets the zen of sold items which the owner didn't collect yet.
    /// </summary>
    public long PendingZen { get; set; }

    /// <summary>
    /// Gets or sets the coins of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingCoins { get; set; }

    /// <summary>
    /// Gets or sets the Jewels of Bless of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingBless { get; set; }

    /// <summary>
    /// Gets or sets the Jewels of Soul of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingSoul { get; set; }

    /// <summary>
    /// Gets or sets the Jewels of Chaos of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingChaos { get; set; }

    /// <summary>
    /// Gets or sets the Jewels of Life of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingLife { get; set; }

    /// <summary>
    /// Gets or sets the Jewels of Creation of sold items which the owner didn't collect yet.
    /// </summary>
    public int PendingCreation { get; set; }

    /// <summary>
    /// Gets or sets the listings of the store.
    /// </summary>
    [MemberOfAggregate]
    public virtual ICollection<MarketListing> Listings { get; protected set; } = null!;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Market store of {this.OwnerName}";
    }
}
