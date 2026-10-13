// <copyright file="ItemCombinationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.Vip;
using MUnique.OpenMU.GameLogic.Vip;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the <see cref="ItemCombinationAction"/>: two equal excellent items become one with new excellent options.
/// </summary>
[TestFixture]
public class ItemCombinationTest
{
    private const byte SourceSlot = 14;
    private const byte TargetSlot = 12;

    private readonly ItemCombinationAction _action = new();

    /// <summary>
    /// Tests that two equal excellent items are combined: the new item is in the target slot, the dragged one is
    /// gone, and the coins are taken.
    /// </summary>
    [Test]
    public async Task CombineAsync()
    {
        var definition = CreateDefinition();
        var player = await CreatePlayerAsync(isVip: true, coins: 40).ConfigureAwait(false);
        await AddItemAsync(player, definition, TargetSlot, level: 7, excellentOptions: 1, luck: false, skill: false).ConfigureAwait(false);
        await AddItemAsync(player, definition, SourceSlot, level: 7, excellentOptions: 2, luck: false, skill: true).ConfigureAwait(false);

        await this._action.CheckAsync(player, SourceSlot, TargetSlot).ConfigureAwait(false);
        GetView(player).Verify(v => v.ShowItemCombinationCheckAsync(ItemCombinationStatus.Ok, SourceSlot, TargetSlot, 15, 40), Times.Once);

        await this._action.CombineAsync(player, SourceSlot, TargetSlot).ConfigureAwait(false);

        GetView(player).Verify(v => v.ShowItemCombinationResultAsync(ItemCombinationStatus.Ok), Times.Once);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(25));
        Assert.That(player.Inventory!.GetItem(SourceSlot), Is.Null);
        var item = player.Inventory.GetItem(TargetSlot)!;
        Assert.That(item.Definition, Is.SameAs(definition));
        Assert.That(item.Level, Is.EqualTo(7));
        Assert.That(item.HasSkill, Is.True, "skill of the dragged item");
        Assert.That(item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.True, "always with luck");
        Assert.That(item.ItemOptions.Count(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent), Is.InRange(1, 3));
    }

    /// <summary>
    /// Tests that the items stay as they are when they can't be combined.
    /// </summary>
    /// <param name="isVip">Whether the account is VIP.</param>
    /// <param name="coins">The coins of the account.</param>
    /// <param name="sourceLevel">The level of the dragged item.</param>
    /// <param name="sourceExcellentOptions">The excellent options of the dragged item.</param>
    /// <param name="expected">The expected status.</param>
    [TestCase(false, 100, 7, 1, ItemCombinationStatus.NotVip)]
    [TestCase(true, 14, 7, 1, ItemCombinationStatus.NotEnoughCoins)]
    [TestCase(true, 100, 6, 1, ItemCombinationStatus.InvalidItems)]
    [TestCase(true, 100, 7, 0, ItemCombinationStatus.InvalidItems)]
    public async Task NotCombinedAsync(bool isVip, int coins, int sourceLevel, int sourceExcellentOptions, ItemCombinationStatus expected)
    {
        var definition = CreateDefinition();
        var player = await CreatePlayerAsync(isVip, coins).ConfigureAwait(false);
        var target = await AddItemAsync(player, definition, TargetSlot, level: 7, excellentOptions: 1, luck: false, skill: false).ConfigureAwait(false);
        var source = await AddItemAsync(player, definition, SourceSlot, (byte)sourceLevel, sourceExcellentOptions, luck: false, skill: false).ConfigureAwait(false);

        await this._action.CombineAsync(player, SourceSlot, TargetSlot).ConfigureAwait(false);

        GetView(player).Verify(v => v.ShowItemCombinationResultAsync(expected), Times.Once);
        Assert.That(player.Inventory!.GetItem(TargetSlot), Is.SameAs(target));
        Assert.That(player.Inventory.GetItem(SourceSlot), Is.SameAs(source));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(coins));
    }

    /// <summary>
    /// Tests that different items can't be combined, and that equipped items aren't used.
    /// </summary>
    [Test]
    public async Task DifferentOrEquippedItemsAreNotCombinedAsync()
    {
        var player = await CreatePlayerAsync(isVip: true, coins: 100).ConfigureAwait(false);
        await AddItemAsync(player, CreateDefinition(), TargetSlot, 7, 1, false, false).ConfigureAwait(false);
        await AddItemAsync(player, CreateDefinition(), SourceSlot, 7, 1, false, false).ConfigureAwait(false);

        await this._action.CombineAsync(player, SourceSlot, TargetSlot).ConfigureAwait(false);
        await this._action.CombineAsync(player, InventoryConstants.ArmorSlot, TargetSlot).ConfigureAwait(false);

        GetView(player).Verify(v => v.ShowItemCombinationResultAsync(ItemCombinationStatus.InvalidItems), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that a combined item has 1 to 3 excellent options, and all of them in some combinations.
    /// </summary>
    [Test]
    public void ExcellentOptionsAreLimitedToThree()
    {
        var definition = CreateDefinition();
        var context = new MUnique.OpenMU.Persistence.InMemory.InMemoryPersistenceContextProvider().CreateNewContext();
        var first = CreateItem(definition, 9, 6, true, true);
        var second = CreateItem(definition, 9, 6, true, true);
        var counts = new HashSet<int>();
        for (var i = 0; i < 300; i++)
        {
            var item = this._action.CreateCombinedItem(first, second, new VipConfiguration(), 3, context);
            var count = item.ItemOptions.Count(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent);
            Assert.That(count, Is.InRange(1, ItemCombinationAction.MaximumExcellentOptions));
            Assert.That(item.ItemOptions.Select(o => o.ItemOption).Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(item.ItemOptions.Count), "no option twice");
            counts.Add(count);
        }

        Assert.That(counts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    private static Mock<IShowVipPlugIn> GetView(Player player)
    {
        return Mock.Get(player.ViewPlugIns.GetPlugIn<IShowVipPlugIn>()!);
    }

    private static async ValueTask<Player> CreatePlayerAsync(bool isVip, int coins)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new VipPlugIn { Configuration = new VipConfiguration() }, true);
        player.Account!.WCoinC = coins;
        player.Account.VipUntil = isVip ? DateTime.UtcNow.AddDays(1) : null;
        return player;
    }

    private static async ValueTask<Item> AddItemAsync(Player player, ItemDefinition definition, byte slot, byte level, int excellentOptions, bool luck, bool skill)
    {
        var item = CreateItem(definition, level, excellentOptions, luck, skill);
        Assert.That(await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false), Is.True);
        return item;
    }

    private static Item CreateItem(ItemDefinition definition, byte level, int excellentOptions, bool luck, bool skill)
    {
        var item = new BasicModel.Item { Definition = definition, Level = level, HasSkill = skill, Durability = 30 };
        foreach (var option in definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).Where(o => o.OptionType == ItemOptionTypes.Excellent).Take(excellentOptions))
        {
            item.ItemOptions.Add(new BasicModel.ItemOptionLink { ItemOption = option });
        }

        if (luck)
        {
            item.ItemOptions.Add(new BasicModel.ItemOptionLink { ItemOption = definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).First(o => o.OptionType == ItemOptionTypes.Luck) });
        }

        return item;
    }

    private static ItemDefinition CreateDefinition()
    {
        var definition = new BasicModel.ItemDefinition { Width = 2, Height = 2, Group = 8, Number = 1, Durability = 30, Skill = new BasicModel.Skill() };
        definition.ItemSlot = new BasicModel.ItemSlotType();
        definition.QualifiedCharacters.Add(new BasicModel.CharacterClass());
        var excellent = new BasicModel.ItemOptionDefinition { MaximumOptionsPerItem = 6, AddChance = 0.001f };
        for (var i = 1; i <= 6; i++)
        {
            excellent.PossibleOptions.Add(new BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.Excellent, Number = i });
        }

        var luck = new BasicModel.ItemOptionDefinition { MaximumOptionsPerItem = 1, AddChance = 0.25f, AddsRandomly = true };
        luck.PossibleOptions.Add(new BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.Luck, Number = 1 });
        var additional = new BasicModel.ItemOptionDefinition { MaximumOptionsPerItem = 1, AddChance = 0.25f, AddsRandomly = true };
        additional.PossibleOptions.Add(new BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.Option, Number = 1 });
        definition.PossibleItemOptions.Add(excellent);
        definition.PossibleItemOptions.Add(luck);
        definition.PossibleItemOptions.Add(additional);
        return definition;
    }
}
