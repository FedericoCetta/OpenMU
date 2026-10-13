// <copyright file="MarketTypes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>
/// The result of a market action, as the MU GKZ client knows it.
/// </summary>
public enum MarketResult : byte
{
    /// <summary>It worked.</summary>
    Success = 0,

    /// <summary>The store isn't open; it has to be opened (paid) first.</summary>
    StoreClosed = 1,

    /// <summary>The store is already open.</summary>
    StoreAlreadyOpen = 2,

    /// <summary>Not enough zen.</summary>
    NotEnoughZen = 3,

    /// <summary>Not enough coins.</summary>
    NotEnoughCoins = 4,

    /// <summary>The store has the most items for sale already.</summary>
    LimitReached = 5,

    /// <summary>This item can't be sold in the market (equipped, bound or not tradable).</summary>
    ItemNotAllowed = 6,

    /// <summary>The price isn't valid.</summary>
    InvalidPrice = 7,

    /// <summary>The inventory has no room for the item.</summary>
    InventoryFull = 8,

    /// <summary>The item isn't for sale any more.</summary>
    NotFound = 9,

    /// <summary>A player can't buy own items.</summary>
    OwnListing = 10,

    /// <summary>Nothing to collect.</summary>
    NothingToCollect = 11,

    /// <summary>Something went wrong; nothing changed.</summary>
    Failed = 12,
}

/// <summary>
/// The kinds of items, to filter the market.
/// </summary>
public enum MarketCategory : byte
{
    /// <summary>All items.</summary>
    All = 0,

    /// <summary>Swords, axes, maces, spears, bows, staffs (groups 0 to 5).</summary>
    Weapons = 1,

    /// <summary>Shields (group 6).</summary>
    Shields = 2,

    /// <summary>Helms, armors, pants, gloves, boots (groups 7 to 11).</summary>
    Armors = 3,

    /// <summary>Wings, orbs and scrolls (groups 12 and 15).</summary>
    WingsAndSkills = 4,

    /// <summary>Pets, rings, pendants and other accessories (group 13).</summary>
    Accessories = 5,

    /// <summary>Jewels, potions and everything else (group 14).</summary>
    Others = 6,
}

/// <summary>
/// The filter of the market list.
/// </summary>
/// <param name="Name">A part of the item name; empty for all.</param>
/// <param name="Category">The kind of item.</param>
/// <param name="ExcellentOnly">Whether only excellent items are listed.</param>
/// <param name="MinimumLevel">The lowest item level (+0 to +15).</param>
public sealed record MarketFilter(string Name, MarketCategory Category, bool ExcellentOnly, byte MinimumLevel)
{
    /// <summary>
    /// Gets a filter which lists everything.
    /// </summary>
    public static MarketFilter All { get; } = new(string.Empty, MarketCategory.All, false, 0);
}

/// <summary>
/// An item for sale, as the market shows it.
/// </summary>
/// <param name="ListingId">The identifier of the listing.</param>
/// <param name="Seller">The name of the seller.</param>
/// <param name="Item">The item.</param>
/// <param name="Price">The price.</param>
/// <param name="Currency">The currency of the price.</param>
/// <param name="TimeLeft">The time until the listing ends; zero or less when it ended.</param>
public sealed record MarketOffer(Guid ListingId, string Seller, Item Item, long Price, MarketCurrency Currency, TimeSpan TimeLeft);

/// <summary>
/// The store of a player, as the market shows it.
/// </summary>
/// <param name="IsOpen">Whether the store is open (paid).</param>
/// <param name="OpenTimeLeft">The time until the store has to be paid again.</param>
/// <param name="StoreFee">The zen which opening the store costs.</param>
/// <param name="PendingZen">The zen of sales to collect.</param>
/// <param name="PendingCoins">The coins of sales to collect.</param>
/// <param name="MaximumListings">The most items for sale at once.</param>
/// <param name="Listings">The items of the store, for sale or ended.</param>
public sealed record MarketStoreState(bool IsOpen, TimeSpan OpenTimeLeft, int StoreFee, long PendingZen, int PendingCoins, int MaximumListings, IReadOnlyList<MarketOffer> Listings);
