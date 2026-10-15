// <copyright file="VipActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Vip;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.NPC;
using MUnique.OpenMU.GameLogic.Views.Vault;
using MUnique.OpenMU.GameLogic.Views.Vip;

/// <summary>
/// The actions of the VIP menu and of the vault selection.
/// </summary>
public class VipActions
{
    /// <summary>
    /// Opens the first vault of the account, the one which opens when talking to the vault NPC.
    /// </summary>
    /// <param name="player">The player.</param>
    public static async ValueTask OpenVaultAsync(Player player)
    {
        await SwitchVaultAsync(player, 1).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the VIP menu: the coins, the VIP state and the benefits.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask ShowInfoAsync(Player player)
    {
        if (player.Account is not { } account || VipPlugIn.GetConfiguration(player.GameContext) is not { } configuration)
        {
            await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVipBuyResultAsync(VipBuyResult.NotAvailable)).ConfigureAwait(false);
            return;
        }

        // Coins which were granted in the admin panel are added now.
        await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        await this.SendInfoAsync(player, account, configuration).ConfigureAwait(false);
    }

    /// <summary>
    /// Buys one VIP period with coins. While the account is VIP, it can't buy again: the periods don't add up.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="withResetCoins">If set to <c>true</c>, it's paid with GKZ Reset Coins instead of the coin of the configuration.</param>
    public async ValueTask BuyAsync(Player player, bool withResetCoins = false)
    {
        if (player.Account is not { } account || VipPlugIn.GetConfiguration(player.GameContext) is not { } configuration)
        {
            await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVipBuyResultAsync(VipBuyResult.NotAvailable)).ConfigureAwait(false);
            return;
        }

        var coinType = withResetCoins ? CashShopCoinType.GoblinPoints : configuration.CoinType;
        var price = withResetCoins ? configuration.ResetCoinsPrice : configuration.Price;
        if (price <= 0)
        {
            await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVipBuyResultAsync(VipBuyResult.NotAvailable)).ConfigureAwait(false);
            return;
        }

        var result = await player.RunPersistenceExclusiveAsync(async () =>
        {
            await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
            if (account.IsVip())
            {
                return VipBuyResult.AlreadyVip;
            }

            var coins = account.GetCashShopCoins(coinType);
            if (coins < price)
            {
                return VipBuyResult.NotEnoughCoins;
            }

            var previousEnd = account.VipUntil;
            var start = DateTime.UtcNow;
            account.SetCashShopCoins(coinType, coins - price);
            account.VipUntil = start.AddDays(configuration.DaysPerPurchase);

            bool saved;
            try
            {
                saved = await player.SaveProgressAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                player.Logger.LogError(ex, "Couldn't save the VIP purchase of player {player}.", player);
                saved = false;
            }

            if (!saved)
            {
                account.SetCashShopCoins(coinType, coins);
                account.VipUntil = previousEnd;
                return VipBuyResult.Failed;
            }

            return VipBuyResult.Success;
        }).ConfigureAwait(false);

        if (result == VipBuyResult.Success)
        {
            player.Logger.LogInformation("Player {player} bought VIP for {price} {coinType}, VIP until {vipUntil}.", player, price, coinType, account.VipUntil);
            VipPlugIn.UpdateBonuses(player);
        }

        await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVipBuyResultAsync(result)).ConfigureAwait(false);
        await this.SendInfoAsync(player, account, configuration).ConfigureAwait(false);
    }

    /// <summary>
    /// Switches the open vault to another vault of the account.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="number">The number of the vault, 1 to <see cref="AccountVaults.MaximumVaults"/>.</param>
    public async ValueTask SelectVaultAsync(Player player, int number)
    {
        if (player.Vault is null
            || player.OpenedNpc?.Definition.NpcWindow != NpcWindow.VaultStorage
            || player.Account is not { } account
            || number < 1
            || number > account.GetAvailableVaults(player.GameContext))
        {
            return;
        }

        await SwitchVaultAsync(player, number).ConfigureAwait(false);
    }

    private static async ValueTask SwitchVaultAsync(Player player, int number)
    {
        var account = player.Account!;
        var isOpen = player.Vault is not null;
        var storage = account.GetOrCreateVault(number, player.PersistenceContext);
        var warehouseSize = account.IsVaultExtended ? InventoryConstants.WarehouseSize * 2 : InventoryConstants.WarehouseSize;
        player.Vault = new Storage(warehouseSize, storage);
        var vipActive = VipPlugIn.GetConfiguration(player.GameContext) is not null;
        if (!isOpen)
        {
            await player.InvokeViewPlugInAsync<IShowVaultPlugIn>(p => p.ShowVaultAsync()).ConfigureAwait(false);
        }
        else
        {
            // The vault window stays open: opening it again would make the client close it first,
            // which tells the server that the vault was closed. The client clears the vault when the
            // open vault changes, so the selection goes before the items.
            if (vipActive)
            {
                await SendSelectionAsync(player, number).ConfigureAwait(false);
            }

            await player.InvokeViewPlugInAsync<IShowMerchantStoreItemListPlugIn>(p => p.ShowMerchantStoreItemListAsync(storage.Items, StoreKind.Normal)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateVaultMoneyPlugIn>(p => p.UpdateVaultMoneyAsync(true)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateVaultStatePlugIn>(p => p.UpdateStateAsync()).ConfigureAwait(false);
            return;
        }

        if (vipActive)
        {
            await SendSelectionAsync(player, number).ConfigureAwait(false);
        }
    }

    private static async ValueTask SendSelectionAsync(Player player, int number)
    {
        var available = player.Account!.GetAvailableVaults(player.GameContext);
        await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVaultSelectionAsync(number, available)).ConfigureAwait(false);
    }

    private async ValueTask SendInfoAsync(Player player, Account account, VipConfiguration configuration)
    {
        var info = new VipInfo(
            account.GetCashShopCoins(configuration.CoinType),
            account.IsVip() ? account.VipUntil : null,
            configuration,
            account.GetCashShopCoins(CashShopCoinType.GoblinPoints));
        await player.InvokeViewPlugInAsync<IShowVipPlugIn>(p => p.ShowVipInfoAsync(info)).ConfigureAwait(false);
    }
}
