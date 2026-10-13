// <copyright file="MarketService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Threading;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Market;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.Persistence;

/// <summary>
/// MU GKZ: the market of a game server. Players open a store for zen, put items of their inventory for sale
/// for zen or coins, and other players buy them, even while the seller is offline. The money of the sales waits in
/// the store until the seller collects it.
/// </summary>
/// <remarks>
/// The stores are loaded once and kept with their own persistence context; every change is saved right away,
/// under one lock. When an item moves between the market and a player, the side which gives the item away is
/// saved first, so a crash between both saves can lose an item, but never duplicate it.
/// </remarks>
public sealed class MarketService : IDisposable
{
    /// <summary>
    /// The number of items on a page of the market.
    /// </summary>
    public const int PageSize = 8;

    private readonly IGameContext _gameContext;
    private readonly Func<MarketConfiguration> _configuration;
    private readonly Func<DateTime> _now;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IContext? _context;
    private List<MarketStore>? _stores;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketService"/> class.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="configuration">Gets the configuration.</param>
    /// <param name="now">Gets the current time; <see cref="DateTime.UtcNow"/> by default.</param>
    public MarketService(IGameContext gameContext, Func<MarketConfiguration> configuration, Func<DateTime>? now = null)
    {
        this._gameContext = gameContext;
        this._configuration = configuration;
        this._now = now ?? (() => DateTime.UtcNow);
    }

    private MarketConfiguration Configuration => this._configuration();

    /// <summary>
    /// Shows a page of the items for sale.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="page">The page, starting at 0.</param>
    public async ValueTask ShowPageAsync(Player player, MarketFilter filter, int page)
    {
        List<MarketOffer> offers;
        int pageCount;
        await this._lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var now = this._now();
            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            await this.RenewStoresAsync(stores, now).ConfigureAwait(false);
            var ownerId = player.Account?.GetId();
            var all = stores
                .Where(s => s.PaidUntil > now)
                .SelectMany(s => s.Listings.Where(l => l.ExpiresAt > now && GetItem(l) is { } item && Matches(item, filter)).Select(l => (Store: s, Listing: l)))
                .OrderByDescending(e => e.Listing.CreatedAt)
                .ToList();
            pageCount = Math.Max(1, (all.Count + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 0, pageCount - 1);
            offers = all.Skip(page * PageSize).Take(PageSize).Select(e => ToOffer(e.Store, e.Listing, now) with { IsOwn = e.Store.AccountId == ownerId }).ToList();
        }
        finally
        {
            this._lock.Release();
        }

        await player.InvokeViewPlugInAsync<IShowMarketPlugIn>(p => p.ShowMarketPageAsync(offers, page, pageCount)).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the store of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask ShowStoreAsync(Player player)
    {
        MarketStoreState state;
        await this._lock.WaitAsync().ConfigureAwait(false);
        try
        {
            state = await this.GetStoreStateAsync(player).ConfigureAwait(false);
        }
        finally
        {
            this._lock.Release();
        }

        await player.InvokeViewPlugInAsync<IShowMarketPlugIn>(p => p.ShowMarketStoreAsync(state)).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens the store of the player, for the fee in zen.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The result.</returns>
    public ValueTask<MarketResult> OpenStoreAsync(Player player)
    {
        return this.RunAsync(player, async () =>
        {
            if (player.Account is not { } account)
            {
                return MarketResult.Failed;
            }

            var now = this._now();
            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            await this.RenewStoresAsync(stores, now).ConfigureAwait(false);
            var store = stores.FirstOrDefault(s => s.AccountId == account.GetId());
            if (store?.PaidUntil > now)
            {
                return MarketResult.StoreAlreadyOpen;
            }

            if (!player.TryRemoveMoney(this.Configuration.StoreFee))
            {
                return MarketResult.NotEnoughZen;
            }

            if (store is null)
            {
                store = this._context!.CreateNew<MarketStore>();
                store.AccountId = account.GetId();
                stores.Add(store);
            }

            store.OwnerName = player.Name;
            store.PaidUntil = now.AddDays(this.Configuration.StoreDays);
            if (!await this.TrySaveMarketAsync().ConfigureAwait(false))
            {
                player.TryAddMoney(this.Configuration.StoreFee);
                return MarketResult.Failed;
            }

            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
            await SavePlayerAsync(player).ConfigureAwait(false);
            player.Logger.LogInformation("Player {player} opened the market store until {paidUntil}.", player, store.PaidUntil);
            return MarketResult.Success;
        });
    }

    /// <summary>
    /// Puts an item of the inventory for sale.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The inventory slot of the item.</param>
    /// <param name="price">The price.</param>
    /// <param name="currency">The currency of the price.</param>
    /// <returns>The result.</returns>
    public ValueTask<MarketResult> SellAsync(Player player, byte slot, long price, MarketCurrency currency)
    {
        return this.RunAsync(player, async () =>
        {
            if (player.Account is not { } account || player.Inventory is not { } inventory)
            {
                return MarketResult.Failed;
            }

            var now = this._now();
            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            await this.RenewStoresAsync(stores, now).ConfigureAwait(false);
            if (stores.FirstOrDefault(s => s.AccountId == account.GetId()) is not { } store || store.PaidUntil <= now)
            {
                return MarketResult.StoreClosed;
            }

            if (store.Listings.Count(l => l.ExpiresAt > now) >= this.GetMaximumListings(account))
            {
                return MarketResult.LimitReached;
            }

            var maximumPrice = currency switch
            {
                MarketCurrency.Coins => this.Configuration.MaximumCoinPrice,
                _ when currency.IsJewel() => this.Configuration.MaximumJewelPrice,
                _ => this.Configuration.MaximumZenPrice,
            };
            if (price <= 0 || price > maximumPrice || !Enum.IsDefined(currency))
            {
                return MarketResult.InvalidPrice;
            }

            if (slot < InventoryConstants.EquippableSlotsCount
                || inventory.GetItem(slot) is not { Definition: { } definition } item
                || !definition.IsTradable
                || definition.IsBoundToCharacter
                || player.PlayerState.CurrentState == PlayerState.TradeOpened
                || player.PlayerState.CurrentState == PlayerState.TradeButtonPressed)
            {
                return MarketResult.ItemNotAllowed;
            }

            // The item leaves the player first: the player is saved before the market gets it.
            await inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            if (!await SavePlayerAsync(player).ConfigureAwait(false))
            {
                await RestoreItemAsync(player, item, slot).ConfigureAwait(false);
                return MarketResult.Failed;
            }

            var storage = this._context!.CreateNew<ItemStorage>();
            var marketItem = this._context.CreateNew<Item>();
            marketItem.AssignValues(item);
            marketItem.ItemSlot = 0;
            storage.Items.Add(marketItem);
            var listing = this._context.CreateNew<MarketListing>();
            listing.Storage = storage;
            listing.Price = price;
            listing.Currency = currency;
            listing.CreatedAt = now;
            listing.ExpiresAt = now.AddDays(this.Configuration.ListingDays);
            store.Listings.Add(listing);
            if (!await this.TrySaveMarketAsync().ConfigureAwait(false))
            {
                player.Logger.LogError("Player {player} lost the item {item}: the market couldn't save it.", player, item);
                return MarketResult.Failed;
            }

            player.Logger.LogInformation("Player {player} put {item} for sale for {price} {currency}.", player, item, price, currency);
            return MarketResult.Success;
        });
    }

    /// <summary>
    /// Buys an item for sale.
    /// </summary>
    /// <param name="player">The buyer.</param>
    /// <param name="listingId">The identifier of the listing.</param>
    /// <returns>The result.</returns>
    public ValueTask<MarketResult> BuyAsync(Player player, Guid listingId)
    {
        return this.RunAsync(player, async () =>
        {
            if (player.Account is not { } account || player.Inventory is not { } inventory)
            {
                return MarketResult.Failed;
            }

            var now = this._now();
            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            await this.RenewStoresAsync(stores, now).ConfigureAwait(false);
            if (FindListing(stores, listingId) is not var (store, listing)
                || store.PaidUntil <= now
                || listing.ExpiresAt <= now
                || GetItem(listing) is not { } marketItem)
            {
                return MarketResult.NotFound;
            }

            if (store.AccountId == account.GetId())
            {
                return MarketResult.OwnListing;
            }

            switch (listing.Currency)
            {
                case MarketCurrency.Zen when player.Money < listing.Price:
                    return MarketResult.NotEnoughZen;
                case MarketCurrency.Coins when account.WCoinC < listing.Price:
                    return MarketResult.NotEnoughCoins;
                case var jewel when jewel.IsJewel() && CountJewels(player, jewel) < listing.Price:
                    return MarketResult.NotEnoughJewels;
            }

            if (inventory.CheckInvSpace(marketItem) is null)
            {
                return MarketResult.InventoryFull;
            }

            // The market gives the item away first: it's saved before the buyer gets the item.
            var item = player.PersistenceContext.CreateNew<Item>();
            item.AssignValues(marketItem);
            await this.RemoveListingAsync(store, listing).ConfigureAwait(false);
            if (listing.Currency == MarketCurrency.Zen)
            {
                store.PendingZen += listing.Price;
            }
            else if (listing.Currency == MarketCurrency.Coins)
            {
                store.PendingCoins += (int)listing.Price;
            }
            else
            {
                store.AddPendingJewels(listing.Currency, (int)listing.Price);
            }

            if (!await this.TrySaveMarketAsync().ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                await this.ReloadAsync().ConfigureAwait(false);
                return MarketResult.Failed;
            }

            if (listing.Currency == MarketCurrency.Zen)
            {
                player.TryRemoveMoney((int)listing.Price);
                await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
            }
            else if (listing.Currency == MarketCurrency.Coins)
            {
                account.WCoinC -= (int)listing.Price;
            }
            else
            {
                await TakeJewelsAsync(player, listing.Currency, (int)listing.Price).ConfigureAwait(false);
            }

            await inventory.AddItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            await SavePlayerAsync(player).ConfigureAwait(false);
            player.Logger.LogInformation("Player {player} bought {item} from {seller} for {price} {currency}.", player, item, store.OwnerName, listing.Price, listing.Currency);
            return MarketResult.Success;
        });
    }

    /// <summary>
    /// Takes an item of the own store back into the inventory: one for sale, or one whose time ended.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="listingId">The identifier of the listing.</param>
    /// <returns>The result.</returns>
    public ValueTask<MarketResult> WithdrawAsync(Player player, Guid listingId)
    {
        return this.RunAsync(player, async () =>
        {
            if (player.Account is not { } account || player.Inventory is not { } inventory)
            {
                return MarketResult.Failed;
            }

            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            if (FindListing(stores, listingId) is not var (store, listing) || store.AccountId != account.GetId() || GetItem(listing) is not { } marketItem)
            {
                return MarketResult.NotFound;
            }

            if (inventory.CheckInvSpace(marketItem) is null)
            {
                return MarketResult.InventoryFull;
            }

            var item = player.PersistenceContext.CreateNew<Item>();
            item.AssignValues(marketItem);
            await this.RemoveListingAsync(store, listing).ConfigureAwait(false);
            if (!await this.TrySaveMarketAsync().ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                await this.ReloadAsync().ConfigureAwait(false);
                return MarketResult.Failed;
            }

            await inventory.AddItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            await SavePlayerAsync(player).ConfigureAwait(false);
            return MarketResult.Success;
        });
    }

    /// <summary>
    /// Collects the zen and coins of the sales of the own store.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The result.</returns>
    public ValueTask<MarketResult> CollectAsync(Player player)
    {
        return this.RunAsync(player, async () =>
        {
            if (player.Account is not { } account)
            {
                return MarketResult.Failed;
            }

            var stores = await this.GetStoresAsync().ConfigureAwait(false);
            await this.RenewStoresAsync(stores, this._now()).ConfigureAwait(false);
            if (stores.FirstOrDefault(s => s.AccountId == account.GetId()) is not { } store)
            {
                return MarketResult.NothingToCollect;
            }

            // Only as much zen as the inventory can hold; the rest stays for later. Jewels go to the jewel bank.
            var zen = (int)Math.Min(store.PendingZen, Math.Max(0, this._gameContext.Configuration.MaximumInventoryMoney - player.Money));
            var coins = store.PendingCoins;
            var jewels = store.GetPendingJewels().Where(j => j.Value > 0).ToList();
            if (zen <= 0 && coins <= 0 && jewels.Count == 0)
            {
                return MarketResult.NothingToCollect;
            }

            store.PendingZen -= zen;
            store.PendingCoins = 0;
            jewels.ForEach(j => store.AddPendingJewels(j.Key, -j.Value));
            if (!await this.TrySaveMarketAsync().ConfigureAwait(false))
            {
                store.PendingZen += zen;
                store.PendingCoins = coins;
                jewels.ForEach(j => store.AddPendingJewels(j.Key, j.Value));
                return MarketResult.Failed;
            }

            player.TryAddMoney(zen);
            account.WCoinC += coins;
            foreach (var (jewel, count) in jewels)
            {
                AddToJewelBank(player, account, jewel, count);
            }
            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
            await SavePlayerAsync(player).ConfigureAwait(false);
            player.Logger.LogInformation("Player {player} collected {zen} zen and {coins} coins from the market.", player, zen, coins);
            return MarketResult.Success;
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._context?.Dispose();
        this._context = null;
        this._lock.Dispose();
    }

    private static Item? GetItem(MarketListing listing) => listing.Storage?.Items.FirstOrDefault();

    private static bool IsJewelItem(Item item, (byte Group, short Number) jewel)
    {
        return item.Definition is { } definition && definition.Group == jewel.Group && definition.Number == jewel.Number && item.Level == 0
               && item.ItemSlot >= InventoryConstants.EquippableSlotsCount;
    }

    private static int Pieces(Item item) => item.IsStackable() ? Math.Max(1, (int)item.Durability) : 1;

    /// <summary>
    /// Counts the jewels of the player: in the inventory and in the jewel bank.
    /// </summary>
    private static long CountJewels(Player player, MarketCurrency currency)
    {
        var jewel = MarketJewels.Items[currency];
        var inInventory = player.Inventory?.Items.Where(i => IsJewelItem(i, jewel)).Sum(Pieces) ?? 0;
        var inBank = player.Account?.BankItems.Where(b => b.ItemGroup == jewel.Group && b.ItemNumber == jewel.Number && b.ItemLevel == 0).Sum(b => (long)b.Count) ?? 0;
        return inInventory + inBank;
    }

    /// <summary>
    /// Takes jewels from the player: from the inventory first, the smallest stacks first, then from the jewel bank.
    /// </summary>
    private static async ValueTask TakeJewelsAsync(Player player, MarketCurrency currency, int count)
    {
        var jewel = MarketJewels.Items[currency];
        var remaining = count;
        foreach (var item in player.Inventory!.Items.Where(i => IsJewelItem(i, jewel)).OrderBy(Pieces).ToList())
        {
            var pieces = Pieces(item);
            if (pieces <= remaining)
            {
                var slot = item.ItemSlot;
                await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
                remaining -= pieces;
            }
            else
            {
                item.Durability -= remaining;
                remaining = 0;
                await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(item, false)).ConfigureAwait(false);
            }

            if (remaining == 0)
            {
                return;
            }
        }

        foreach (var bankItem in player.Account!.BankItems.Where(b => b.ItemGroup == jewel.Group && b.ItemNumber == jewel.Number && b.ItemLevel == 0))
        {
            var taken = Math.Min(remaining, bankItem.Count);
            bankItem.Count -= taken;
            remaining -= taken;
            if (remaining == 0)
            {
                return;
            }
        }
    }

    private static void AddToJewelBank(Player player, Account account, MarketCurrency currency, int count)
    {
        var jewel = MarketJewels.Items[currency];
        var bankItem = account.BankItems.FirstOrDefault(b => b.ItemGroup == jewel.Group && b.ItemNumber == jewel.Number && b.ItemLevel == 0);
        if (bankItem is null)
        {
            bankItem = player.PersistenceContext.CreateNew<AccountBankItem>();
            bankItem.ItemGroup = jewel.Group;
            bankItem.ItemNumber = jewel.Number;
            account.BankItems.Add(bankItem);
        }

        bankItem.Count += count;
    }

    private static (MarketStore Store, MarketListing Listing)? FindListing(IEnumerable<MarketStore> stores, Guid listingId)
    {
        foreach (var store in stores)
        {
            if (store.Listings.FirstOrDefault(l => l.Id == listingId) is { } listing)
            {
                return (store, listing);
            }
        }

        return null;
    }

    private static MarketOffer ToOffer(MarketStore store, MarketListing listing, DateTime now)
    {
        return new MarketOffer(listing.Id, store.OwnerName, GetItem(listing)!, listing.Price, listing.Currency, listing.ExpiresAt - now);
    }

    private static bool Matches(Item item, MarketFilter filter)
    {
        if (item.Definition is not { } definition || item.Level < filter.MinimumLevel || (filter.ExcellentOnly && !item.IsExcellent()))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.Name)
            && definition.Name.ValueInNeutralLanguage.IndexOf(filter.Name.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return filter.Category switch
        {
            MarketCategory.All => true,
            MarketCategory.Weapons => definition.Group <= 5,
            MarketCategory.Shields => definition.Group == 6,
            MarketCategory.Armors => definition.Group is >= 7 and <= 11,
            MarketCategory.WingsAndSkills => definition.Group is 12 or 15,
            MarketCategory.Accessories => definition.Group == 13,
            MarketCategory.Others => definition.Group == 14,
            _ => true,
        };
    }

    private static async ValueTask<bool> SavePlayerAsync(Player player)
    {
        try
        {
            return await player.SaveProgressAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Couldn't save player {player} after a market action.", player);
            return false;
        }
    }

    private static async ValueTask RestoreItemAsync(Player player, Item item, byte slot)
    {
        if (await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false))
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        }
    }

    private int GetMaximumListings(Account account)
    {
        return account.IsVip() ? this.Configuration.MaximumListingsVip : this.Configuration.MaximumListings;
    }

    private async ValueTask<MarketResult> RunAsync(Player player, Func<ValueTask<MarketResult>> action)
    {
        MarketResult result;
        await this._lock.WaitAsync().ConfigureAwait(false);
        try
        {
            result = await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Market action of player {player} failed.", player);
            result = MarketResult.Failed;
        }
        finally
        {
            this._lock.Release();
        }

        await player.InvokeViewPlugInAsync<IShowMarketPlugIn>(p => p.ShowMarketResultAsync(result)).ConfigureAwait(false);
        await this.ShowStoreAsync(player).ConfigureAwait(false);
        return result;
    }

    private async ValueTask<MarketStoreState> GetStoreStateAsync(Player player)
    {
        var now = this._now();
        var configuration = this.Configuration;
        var stores = await this.GetStoresAsync().ConfigureAwait(false);
        await this.RenewStoresAsync(stores, now).ConfigureAwait(false);
        var store = player.Account is { } account ? stores.FirstOrDefault(s => s.AccountId == account.GetId()) : null;
        var maximum = player.Account is { } owner ? this.GetMaximumListings(owner) : configuration.MaximumListings;
        if (store is null)
        {
            return new MarketStoreState(false, TimeSpan.Zero, configuration.StoreFee, 0, 0, new Dictionary<MarketCurrency, int>(), maximum, []);
        }

        var listings = store.Listings
            .Where(l => GetItem(l) is not null)
            .OrderBy(l => l.ExpiresAt)
            .Select(l => ToOffer(store, l, now) with { IsOwn = true })
            .ToList();
        return new MarketStoreState(store.PaidUntil > now, store.PaidUntil - now, configuration.StoreFee, store.PendingZen, store.PendingCoins, store.GetPendingJewels(), maximum, listings);
    }

    /// <summary>
    /// Charges the stores whose paid time ended again from the zen of their sales, as long as they still have items
    /// for sale; the others close until their owner opens them again.
    /// </summary>
    private async ValueTask RenewStoresAsync(IEnumerable<MarketStore> stores, DateTime now)
    {
        var fee = this.Configuration.StoreFee;
        var changed = false;
        foreach (var store in stores.Where(s => s.PaidUntil <= now && s.PendingZen >= fee && s.Listings.Any(l => l.ExpiresAt > now)))
        {
            store.PendingZen -= fee;
            store.PaidUntil = now.AddDays(this.Configuration.StoreDays);
            changed = true;
        }

        if (changed)
        {
            await this.TrySaveMarketAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask RemoveListingAsync(MarketStore store, MarketListing listing)
    {
        store.Listings.Remove(listing);
        if (listing.Storage is { } storage)
        {
            foreach (var item in storage.Items.ToList())
            {
                await this._context!.DeleteAsync(item).ConfigureAwait(false);
            }

            await this._context!.DeleteAsync(storage).ConfigureAwait(false);
        }

        await this._context!.DeleteAsync(listing).ConfigureAwait(false);
    }

    private async ValueTask<List<MarketStore>> GetStoresAsync()
    {
        if (this._stores is null)
        {
            this._context = this._gameContext.PersistenceContextProvider.CreateNewTypedContext(typeof(MarketStore), false, this._gameContext.Configuration);
            this._stores = (await this._context.GetAsync<MarketStore>().ConfigureAwait(false)).ToList();
        }

        return this._stores;
    }

    private async ValueTask<bool> TrySaveMarketAsync()
    {
        try
        {
            return await this._context!.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._gameContext.LoggerFactory.CreateLogger<MarketService>().LogError(ex, "Couldn't save the market.");
            return false;
        }
    }

    /// <summary>
    /// Drops the loaded stores after a failed save, so the next action works with what's really saved.
    /// </summary>
    private ValueTask ReloadAsync()
    {
        this._context?.Dispose();
        this._context = null;
        this._stores = null;
        return ValueTask.CompletedTask;
    }
}
