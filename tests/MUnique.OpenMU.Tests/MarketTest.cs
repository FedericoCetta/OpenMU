// <copyright file="MarketTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Market;
using MUnique.OpenMU.GameLogic.Views.Market;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the market of MU GKZ.
/// </summary>
[TestFixture]
public class MarketTest
{
    private const int Fee = 5_000_000;
    private MarketConfiguration _configuration = null!;
    private DateTime _now;
    private ItemDefinition _armor = null!;
    private Player _seller = null!;
    private Player _buyer = null!;
    private MarketService _market = null!;

    /// <summary>
    /// Creates a seller and a buyer on the same server.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._configuration = new MarketConfiguration();
        this._now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        this._seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        this._buyer = await PlayerTestHelper.CreatePlayerAsync(this._seller.GameContext).ConfigureAwait(false);
        this._seller.Account = new BasicModel.Account { Id = Guid.NewGuid(), LoginName = "seller" };
        this._buyer.Account = new BasicModel.Account { Id = Guid.NewGuid(), LoginName = "buyer" };
        this._seller.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        this._armor = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 8, Number = 1, Width = 2, Height = 2, Durability = 30, Name = "Dragon Armor" };
        this._armor.ItemSlot = new BasicModel.ItemSlotType();
        this._seller.GameContext.Configuration.Items.Add(this._armor);
        this._market = new MarketService(this._seller.GameContext, () => this._configuration, () => this._now);
    }

    /// <summary>
    /// Disposes the market.
    /// </summary>
    [TearDown]
    public void TearDown() => this._market.Dispose();

    /// <summary>
    /// Tests that opening the store costs its fee, and that it can't be opened twice.
    /// </summary>
    [Test]
    public async Task OpeningTheStoreCostsTheFeeAsync()
    {
        this._seller.Money = Fee + 1000;

        Assert.That(await this._market.OpenStoreAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._seller.Money, Is.EqualTo(1000));
        Assert.That(await this._market.OpenStoreAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.StoreAlreadyOpen));

        this._seller.Money = 0;
        Assert.That(await new MarketService(this._seller.GameContext, () => this._configuration, () => this._now).OpenStoreAsync(this._buyer).ConfigureAwait(false), Is.EqualTo(MarketResult.NotEnoughZen));
    }

    /// <summary>
    /// Tests that an item can't be sold without an open store.
    /// </summary>
    [Test]
    public async Task SellingNeedsAnOpenStoreAsync()
    {
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);

        Assert.That(await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.StoreClosed));
        Assert.That(this._seller.Inventory!.GetItem(12), Is.Not.Null);
    }

    /// <summary>
    /// Tests a sale for zen: the item goes from the seller to the market and to the buyer, the zen to the store,
    /// and the seller collects it.
    /// </summary>
    [Test]
    public async Task SellAndBuyForZenAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        var item = await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        item.Level = 9;

        Assert.That(await this._market.SellAsync(this._seller, 12, 3000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._seller.Inventory!.GetItem(12), Is.Null);

        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();
        Assert.That((offer.Item.Level, offer.Price, offer.Seller), Is.EqualTo(((byte)9, 3000L, this._seller.Name)));

        this._buyer.Money = 5000;
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._buyer.Money, Is.EqualTo(2000));
        Assert.That(this._buyer.Inventory!.Items.Single(i => i.Definition == this._armor).Level, Is.EqualTo(9));
        Assert.That(await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false), Is.Empty);

        var moneyBefore = this._seller.Money;
        Assert.That(await this._market.CollectAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._seller.Money, Is.EqualTo(moneyBefore + 3000));
        Assert.That(await this._market.CollectAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.NothingToCollect));
    }

    /// <summary>
    /// Tests a sale for coins.
    /// </summary>
    [Test]
    public async Task SellAndBuyForCoinsAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 12, 30, MarketCurrency.Coins).ConfigureAwait(false);
        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();

        this._buyer.Account!.WCoinC = 20;
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.NotEnoughCoins));

        this._buyer.Account.WCoinC = 50;
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._buyer.Account.WCoinC, Is.EqualTo(20));

        await this._market.CollectAsync(this._seller).ConfigureAwait(false);
        Assert.That(this._seller.Account!.WCoinC, Is.EqualTo(30));
    }

    /// <summary>
    /// Tests a sale for jewels: the buyer pays from the inventory first, then from the jewel bank, and the seller
    /// collects them into the jewel bank.
    /// </summary>
    [Test]
    public async Task SellAndBuyForJewelsAsync()
    {
        var bless = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 14, Number = 13, Width = 1, Height = 1, Durability = 50, Name = "Jewel of Bless" };
        this._seller.GameContext.Configuration.Items.Add(bless);
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        Assert.That(await this._market.SellAsync(this._seller, 12, 30, MarketCurrency.Bless).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();
        Assert.That(offer.Currency, Is.EqualTo(MarketCurrency.Bless));

        var stack = new BasicModel.Item { Definition = bless, Durability = 20 };
        await this._buyer.Inventory!.AddItemAsync(20, stack).ConfigureAwait(false);
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.NotEnoughJewels));

        this._buyer.Account!.BankItems.Add(new BasicModel.AccountBankItem { ItemGroup = 14, ItemNumber = 13, Count = 15 });
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._buyer.Inventory.Items, Does.Not.Contain(stack), "the 20 of the inventory first");
        Assert.That(this._buyer.Account.BankItems.Single().Count, Is.EqualTo(5), "then 10 of the bank");

        Assert.That(await this._market.CollectAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._seller.Account!.BankItems.Single(b => b.ItemGroup == 14 && b.ItemNumber == 13).Count, Is.EqualTo(30));
    }

    /// <summary>
    /// Tests that an item keeps its options in the market, also after a restart of the server, and that the buyer
    /// gets it with them.
    /// </summary>
    [Test]
    public async Task ItemsKeepTheirOptionsAfterARestartAsync()
    {
        var excellent = new BasicModel.IncreasableItemOption { Id = Guid.NewGuid(), Number = 1 };
        var definition = new BasicModel.ItemOptionDefinition { Id = Guid.NewGuid() };
        definition.PossibleOptions.Add(excellent);
        Mock.Get(this._seller.GameContext.Configuration).Setup(c => c.ItemOptions).Returns(new List<ItemOptionDefinition> { definition });
        await this.OpenStoreAsync().ConfigureAwait(false);
        var item = await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        item.Level = 11;
        item.HasSkill = true;
        item.ItemOptions.Add(new BasicModel.ItemOptionLink { ItemOption = excellent, Level = 0, Index = 2 });
        Assert.That(await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));

        // A new market loads the listings from the persistence, like after a restart.
        this._market.Dispose();
        this._market = new MarketService(this._seller.GameContext, () => this._configuration, () => this._now);
        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();
        Assert.That((offer.Item.Level, offer.Item.HasSkill, offer.Item.ItemOptions.Single().ItemOption, offer.Item.ItemOptions.Single().Index), Is.EqualTo(((byte)11, true, (IncreasableItemOption)excellent, 2)));

        this._buyer.Money = 1000;
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        var bought = this._buyer.Inventory!.Items.Single(i => i.Definition == this._armor);
        Assert.That((bought.Level, bought.ItemOptions.Single().ItemOption), Is.EqualTo(((byte)11, (IncreasableItemOption)excellent)));
    }

    /// <summary>
    /// Tests that the market marks the own items, so the client doesn't offer to buy them.
    /// </summary>
    [Test]
    public async Task OwnItemsAreMarkedAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false);

        Assert.That((await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single().IsOwn, Is.False);
        Assert.That((await this.GetPageAsync(MarketFilter.All, this._seller).ConfigureAwait(false)).Single().IsOwn, Is.True);
    }

    /// <summary>
    /// Tests that a player can't buy own items, and can take them back.
    /// </summary>
    [Test]
    public async Task OwnItemsCanOnlyBeTakenBackAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false);
        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();

        Assert.That(await this._market.BuyAsync(this._seller, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.OwnListing));
        Assert.That(await this._market.WithdrawAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.NotFound));
        Assert.That(await this._market.WithdrawAsync(this._seller, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(this._seller.Inventory!.Items.Count(i => i.Definition == this._armor), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests the limit of items for sale: 10, or 30 with VIP.
    /// </summary>
    [Test]
    public async Task ListingsAreLimitedAsync()
    {
        this._configuration.MaximumListings = 1;
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller, 12).ConfigureAwait(false);
        await this.AddArmorAsync(this._seller, 14).ConfigureAwait(false);

        Assert.That(await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
        Assert.That(await this._market.SellAsync(this._seller, 14, 1000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.LimitReached));

        this._seller.Account!.VipUntil = DateTime.UtcNow.AddDays(1);
        Assert.That(await this._market.SellAsync(this._seller, 14, 1000, MarketCurrency.Zen).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
    }

    /// <summary>
    /// Tests that after 7 days the item isn't for sale any more, and the seller gets it back.
    /// </summary>
    [Test]
    public async Task ListingsEndAfterSevenDaysAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false);
        var offer = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single();
        this._seller.Money = Fee;

        this._now = this._now.AddDays(7);
        await this._market.OpenStoreAsync(this._seller).ConfigureAwait(false);

        Assert.That(await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false), Is.Empty);
        Assert.That(await this._market.BuyAsync(this._buyer, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.NotFound));
        Assert.That(await this._market.WithdrawAsync(this._seller, offer.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
    }

    /// <summary>
    /// Tests that after 4 days the store is charged again from the zen of its sales, or closes and hides its items.
    /// </summary>
    [Test]
    public async Task StoreIsChargedEveryFourDaysAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        await this.AddArmorAsync(this._seller, 12).ConfigureAwait(false);
        await this.AddArmorAsync(this._seller, 14).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 12, Fee, MarketCurrency.Zen).ConfigureAwait(false);
        await this._market.SellAsync(this._seller, 14, 1000, MarketCurrency.Zen).ConfigureAwait(false);
        this._buyer.Money = Fee;
        var expensive = (await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false)).Single(o => o.Price == Fee);
        Assert.That(await this._market.BuyAsync(this._buyer, expensive.ListingId).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));

        this._now = this._now.AddDays(4);
        Assert.That(await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false), Has.Count.EqualTo(1), "paid from the sale");

        this._now = this._now.AddDays(4);
        Assert.That(await this.GetPageAsync(MarketFilter.All).ConfigureAwait(false), Is.Empty, "nothing left to pay with: closed");
    }

    /// <summary>
    /// Tests the filters of the market.
    /// </summary>
    [Test]
    public async Task FiltersAsync()
    {
        await this.OpenStoreAsync().ConfigureAwait(false);
        var item = await this.AddArmorAsync(this._seller).ConfigureAwait(false);
        item.Level = 11;
        await this._market.SellAsync(this._seller, 12, 1000, MarketCurrency.Zen).ConfigureAwait(false);

        Assert.That(await this.GetPageAsync(new MarketFilter("dragon", MarketCategory.Armors, false, 11)).ConfigureAwait(false), Has.Count.EqualTo(1));
        Assert.That(await this.GetPageAsync(new MarketFilter("sword", MarketCategory.All, false, 0)).ConfigureAwait(false), Is.Empty);
        Assert.That(await this.GetPageAsync(new MarketFilter(string.Empty, MarketCategory.Weapons, false, 0)).ConfigureAwait(false), Is.Empty);
        Assert.That(await this.GetPageAsync(new MarketFilter(string.Empty, MarketCategory.All, false, 12)).ConfigureAwait(false), Is.Empty);
        Assert.That(await this.GetPageAsync(new MarketFilter(string.Empty, MarketCategory.All, true, 0)).ConfigureAwait(false), Is.Empty, "not excellent");
    }

    private async ValueTask OpenStoreAsync()
    {
        this._seller.Money = Fee;
        Assert.That(await this._market.OpenStoreAsync(this._seller).ConfigureAwait(false), Is.EqualTo(MarketResult.Success));
    }

    private async ValueTask<Item> AddArmorAsync(Player player, byte slot = 12)
    {
        var item = new BasicModel.Item { Definition = this._armor, Durability = 30 };
        Assert.That(await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false), Is.True);
        return item;
    }

    private async ValueTask<IReadOnlyList<MarketOffer>> GetPageAsync(MarketFilter filter, Player? player = null)
    {
        player ??= this._buyer;
        IReadOnlyList<MarketOffer> result = [];
        var view = Mock.Get(player.ViewPlugIns.GetPlugIn<IShowMarketPlugIn>()!);
        view.Setup(v => v.ShowMarketPageAsync(It.IsAny<IReadOnlyList<MarketOffer>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<IReadOnlyList<MarketOffer>, int, int>((offers, _, _) => result = offers)
            .Returns(ValueTask.CompletedTask);
        await this._market.ShowPageAsync(player, filter, 0).ConfigureAwait(false);
        return result;
    }
}
