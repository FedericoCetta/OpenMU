// <copyright file="VipConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Vip;

/// <summary>
/// The configuration of the VIP of MU GKZ: what it costs and what it gives.
/// </summary>
public class VipConfiguration
{
    /// <summary>
    /// Gets or sets the price of the VIP, in the coin of <see cref="CoinType"/>.
    /// </summary>
    [Display(Name = "Price", Description = "The price of one VIP period, in the coin of the coin type.")]
    public int Price { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the coin with which the VIP is paid. MU GKZ calls WCoin (C) "GKZ Coins".
    /// </summary>
    [Display(Name = "Coin type", Description = "The coin with which the VIP is paid. They're granted in the admin panel, on the cash shop coins page of the account.")]
    public CashShopCoinType CoinType { get; set; } = CashShopCoinType.WCoinC;

    /// <summary>
    /// Gets or sets the name of the coin which the players see.
    /// </summary>
    [Display(Name = "Coin name", Description = "The name of the coin which the players see in the VIP menu.")]
    public string CoinName { get; set; } = "GKZ Coins";

    /// <summary>
    /// Gets or sets the price of the VIP in GKZ Reset Coins (the Goblin Points of the account, which are earned with resets).
    /// 0 means that the VIP can't be bought with them.
    /// </summary>
    [Display(Name = "Price in Reset Coins", Description = "The price of one VIP period in GKZ Reset Coins (the Goblin Points, earned with resets). 0 means that it can't be bought with them.")]
    public int ResetCoinsPrice { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the number of days which one purchase adds.
    /// </summary>
    [Display(Name = "Days per purchase", Description = "The number of days of one purchase. A VIP account can buy again when its VIP ended.")]
    public int DaysPerPurchase { get; set; } = 30;

    /// <summary>
    /// Gets or sets the additional experience, e.g. 0.45 for 45 % more.
    /// </summary>
    [Display(Name = "Experience bonus", Description = "The additional experience, e.g. 0.45 for 45 % more.")]
    public float ExperienceBonus { get; set; } = 0.45f;

    /// <summary>
    /// Gets or sets the additional chance that a monster drops an item, e.g. 0.05 for 5 percentage points.
    /// </summary>
    [Display(Name = "Item drop chance bonus", Description = "The additional chance that a monster kill drops an item, e.g. 0.05 for 5 percentage points (30 % becomes 35 %).")]
    public float ItemDropChanceBonus { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets the additional zen of money drops, e.g. 0.5 for 50 % more.
    /// </summary>
    [Display(Name = "Zen drop bonus", Description = "The additional zen of money drops, e.g. 0.5 for 50 % more.")]
    public float MoneyBonus { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the number of vaults of accounts without VIP.
    /// </summary>
    [Display(Name = "Vaults without VIP", Description = "The number of vaults of accounts without VIP (1 to 6).")]
    public int NormalVaults { get; set; } = 2;

    /// <summary>
    /// Gets or sets the number of vaults of VIP accounts.
    /// </summary>
    [Display(Name = "Vaults with VIP", Description = "The number of vaults of VIP accounts (1 to 6). The items of vaults above the limit are kept when the VIP ends.")]
    public int VipVaults { get; set; } = AccountVaults.MaximumVaults;

    /// <summary>
    /// Gets or sets the price of an item combination, in the coin of <see cref="CoinType"/>.
    /// </summary>
    [Display(Name = "Item combination price", Description = "The price of combining two equal excellent items into a new one with random excellent options.")]
    public int CombinationPrice { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether only VIP accounts can combine items.
    /// </summary>
    [Display(Name = "Item combination only for VIP", Description = "Whether only VIP accounts can combine items.")]
    public bool CombinationRequiresVip { get; set; } = true;

    /// <summary>
    /// Gets or sets the chance that the combined item gets two excellent options, e.g. 0.3 for 30 %.
    /// </summary>
    [Display(Name = "Item combination: chance of 2 excellent options", Description = "The chance that the new item gets two excellent options, e.g. 0.25 for 25 %. The rest gets one, unless it gets three.")]
    public double CombinationTwoOptionsChance { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the chance that the combined item gets three excellent options, e.g. 0.05 for 5 %. Three is the most.
    /// </summary>
    [Display(Name = "Item combination: chance of 3 excellent options", Description = "The chance that the new item gets three excellent options, e.g. 0.05 for 5 %. Three is the most.")]
    public double CombinationThreeOptionsChance { get; set; } = 0.05;
}
