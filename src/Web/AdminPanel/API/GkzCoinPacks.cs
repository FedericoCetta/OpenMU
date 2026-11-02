// <copyright file="GkzCoinPacks.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// A pack of GKZ Coins which the players buy in the player panel.
/// </summary>
/// <param name="Id">The identifier, which the website sends.</param>
/// <param name="Name">The name, as the payment page shows it.</param>
/// <param name="Coins">The coins of the pack.</param>
/// <param name="BonusCoins">The coins which the pack gives on top of <paramref name="Coins"/>.</param>
/// <param name="PriceUsd">The price in US dollars.</param>
public sealed record GkzCoinPack(string Id, string Name, int Coins, int BonusCoins, decimal PriceUsd)
{
    /// <summary>
    /// Gets all the coins which the pack gives.
    /// </summary>
    public int TotalCoins => this.Coins + this.BonusCoins;
}

/// <summary>
/// MU GKZ: the packs of GKZ Coins which are for sale. The prices are always in US dollars,
/// because the account of Mercado Pago is from Uruguay and PayPal charges in dollars anyway.
/// </summary>
public static class GkzCoinPacks
{
    /// <summary>
    /// Gets the packs, from the cheapest to the most expensive.
    /// </summary>
    public static IReadOnlyList<GkzCoinPack> All { get; } =
    [
        new("inicial", "Pack Inicial", 500, 0, 3m),
        new("vip", "Pack VIP", 1000, 0, 5m),
        new("grande", "Pack Grande", 2500, 250, 9m),
        new("gremio", "Pack Gremio", 5000, 1000, 17m),
    ];

    /// <summary>
    /// Gets a pack by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The pack; <c>null</c>, if there is none with this identifier.</returns>
    public static GkzCoinPack? Find(string? id) => All.FirstOrDefault(p => p.Id == id);
}
