// <copyright file="MarketCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// MU GKZ: the currency of a market price.
/// </summary>
public enum MarketCurrency
{
    /// <summary>
    /// Zen.
    /// </summary>
    Zen = 0,

    /// <summary>
    /// GKZ Coins (WCoinC).
    /// </summary>
    Coins = 1,
}
