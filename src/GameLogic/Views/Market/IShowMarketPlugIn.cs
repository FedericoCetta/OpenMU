// <copyright file="IShowMarketPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Market;

using MUnique.OpenMU.GameLogic.Market;

/// <summary>
/// MU GKZ: shows the market.
/// </summary>
public interface IShowMarketPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows a page of the items for sale.
    /// </summary>
    /// <param name="offers">The items of the page.</param>
    /// <param name="page">The page, starting at 0.</param>
    /// <param name="pageCount">The number of pages.</param>
    ValueTask ShowMarketPageAsync(IReadOnlyList<MarketOffer> offers, int page, int pageCount);

    /// <summary>
    /// Shows the store of the player.
    /// </summary>
    /// <param name="state">The store.</param>
    ValueTask ShowMarketStoreAsync(MarketStoreState state);

    /// <summary>
    /// Shows the result of an action.
    /// </summary>
    /// <param name="result">The result.</param>
    ValueTask ShowMarketResultAsync(MarketResult result);
}
