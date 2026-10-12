// <copyright file="IShowVipPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Vip;

/// <summary>
/// The result of a VIP purchase.
/// </summary>
public enum VipBuyResult
{
    /// <summary>
    /// The VIP was bought.
    /// </summary>
    Success,

    /// <summary>
    /// The account doesn't have enough coins.
    /// </summary>
    NotEnoughCoins,

    /// <summary>
    /// The VIP isn't available on this server.
    /// </summary>
    NotAvailable,

    /// <summary>
    /// The purchase couldn't be saved; nothing was charged.
    /// </summary>
    Failed,
}

/// <summary>
/// The state of the VIP of an account, as the VIP menu shows it.
/// </summary>
/// <param name="Coins">The coins of the account.</param>
/// <param name="VipUntil">The end of the VIP (UTC); <c>null</c> if the account isn't VIP.</param>
/// <param name="Configuration">The configuration with the price and the bonuses.</param>
public record VipInfo(int Coins, DateTime? VipUntil, GameLogic.Vip.VipConfiguration Configuration);

/// <summary>
/// Interface of a view whose implementation shows the VIP menu.
/// </summary>
public interface IShowVipPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the VIP state, the coins and the benefits.
    /// </summary>
    /// <param name="info">The VIP info.</param>
    ValueTask ShowVipInfoAsync(VipInfo info);

    /// <summary>
    /// Shows the result of a VIP purchase.
    /// </summary>
    /// <param name="result">The result.</param>
    ValueTask ShowVipBuyResultAsync(VipBuyResult result);

    /// <summary>
    /// Shows which vault is open and how many vaults can be opened.
    /// </summary>
    /// <param name="currentVault">The number of the open vault, 1 to 6.</param>
    /// <param name="availableVaults">The number of vaults which can be opened.</param>
    ValueTask ShowVaultSelectionAsync(int currentVault, int availableVaults);
}
