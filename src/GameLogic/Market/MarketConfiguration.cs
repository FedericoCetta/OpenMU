// <copyright file="MarketConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>
/// The configuration of the market of MU GKZ.
/// </summary>
public class MarketConfiguration
{
    /// <summary>
    /// Gets or sets the zen which opening the store costs, for <see cref="StoreDays"/>.
    /// </summary>
    public int StoreFee { get; set; } = 5_000_000;

    /// <summary>
    /// Gets or sets the days for which the store is paid; then it's charged again from the zen of its sales, or closes.
    /// </summary>
    public int StoreDays { get; set; } = 4;

    /// <summary>
    /// Gets or sets the days an item stays for sale; then the owner gets it back.
    /// </summary>
    public int ListingDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the most items for sale at once, for accounts without VIP.
    /// </summary>
    public int MaximumListings { get; set; } = 10;

    /// <summary>
    /// Gets or sets the most items for sale at once, for VIP accounts.
    /// </summary>
    public int MaximumListingsVip { get; set; } = 30;

    /// <summary>
    /// Gets or sets the highest price in zen.
    /// </summary>
    public long MaximumZenPrice { get; set; } = 2_000_000_000;

    /// <summary>
    /// Gets or sets the highest price in coins.
    /// </summary>
    public long MaximumCoinPrice { get; set; } = 1_000_000;

    /// <summary>
    /// Gets or sets the highest price in jewels.
    /// </summary>
    public long MaximumJewelPrice { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the tax on the sales, in percent: the seller gets the price without it, rounded in favor of the seller.
    /// It takes zen, coins and jewels out of the economy.
    /// </summary>
    public int SalesTaxPercent { get; set; } = 3;

    /// <summary>
    /// Gets what the seller gets for a sale, without the <see cref="SalesTaxPercent"/>.
    /// </summary>
    /// <param name="price">The price.</param>
    /// <returns>The price without the tax.</returns>
    public long GetSellerShare(long price) => price - (price * Math.Clamp(this.SalesTaxPercent, 0, 100) / 100);
}
