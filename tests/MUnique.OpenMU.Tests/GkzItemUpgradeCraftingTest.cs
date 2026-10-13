// <copyright file="GkzItemUpgradeCraftingTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the item level upgrade of MU GKZ with the Talisman of Chaos Assembly.
/// </summary>
[TestFixture]
public class GkzItemUpgradeCraftingTest
{
    private readonly ItemDefinition _sword = CreateSword();
    private readonly ItemDefinition _chaos = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 12, Number = 15, Width = 1, Height = 1, Durability = 1 };
    private readonly ItemDefinition _talisman = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 14, Number = 96, Width = 1, Height = 1, Durability = 1 };

    /// <summary>
    /// Tests that a failed upgrade to +11 with the talisman takes the item back to +0 and uses up the talisman.
    /// </summary>
    [Test]
    public async Task FailureWithTalismanGoesBackToZeroAsync()
    {
        var player = await this.CreatePlayerAsync().ConfigureAwait(false);
        var sword = await this.AddAsync(player, this._sword, 0, 10).ConfigureAwait(false);
        await this.AddAsync(player, this._chaos, 1).ConfigureAwait(false);
        await this.AddAsync(player, this._talisman, 2).ConfigureAwait(false);

        var (result, _) = await this.CreateHandler(acceptsTalisman: true).DoMixAsync(player, 0).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(CraftingResult.Failed));
        Assert.That(sword.Level, Is.Zero);
        Assert.That(player.TemporaryStorage!.Items, Is.EqualTo(new[] { sword }), "the chaos and the talisman are used up");
    }

    /// <summary>
    /// Tests that a failed upgrade without the talisman destroys the item, as always.
    /// </summary>
    [Test]
    public async Task FailureWithoutTalismanDestroysTheItemAsync()
    {
        var player = await this.CreatePlayerAsync().ConfigureAwait(false);
        await this.AddAsync(player, this._sword, 0, 10).ConfigureAwait(false);
        await this.AddAsync(player, this._chaos, 1).ConfigureAwait(false);

        var (result, _) = await this.CreateHandler(acceptsTalisman: true).DoMixAsync(player, 0).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(CraftingResult.Failed));
        Assert.That(player.TemporaryStorage!.Items, Is.Empty);
    }

    /// <summary>
    /// Tests that an upgrade which doesn't list the talisman (+14, +15) refuses it, without touching the items.
    /// </summary>
    [Test]
    public async Task UpgradeWithoutTalismanRefusesItAsync()
    {
        var player = await this.CreatePlayerAsync().ConfigureAwait(false);
        var sword = await this.AddAsync(player, this._sword, 0, 10).ConfigureAwait(false);
        await this.AddAsync(player, this._chaos, 1).ConfigureAwait(false);
        await this.AddAsync(player, this._talisman, 2).ConfigureAwait(false);

        var (result, _) = await this.CreateHandler(acceptsTalisman: false).DoMixAsync(player, 0).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(CraftingResult.IncorrectMixItems));
        Assert.That(sword.Level, Is.EqualTo(10));
        Assert.That(player.TemporaryStorage!.Items.Count(), Is.EqualTo(3));
    }

    private static ItemDefinition CreateSword()
    {
        var sword = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 0, Number = 1, Width = 1, Height = 3, Durability = 20, MaximumItemLevel = 15 };
        sword.ItemSlot = new BasicModel.ItemSlotType();
        return sword;
    }

    private GkzItemUpgradeCrafting CreateHandler(bool acceptsTalisman)
    {
        // A success chance of 0, so the upgrade always fails.
        var settings = new BasicModel.SimpleCraftingSettings { SuccessPercent = 0 };
        settings.RequiredItems.Add(new BasicModel.ItemCraftingRequiredItem
        {
            Reference = 1, MinimumAmount = 1, MaximumAmount = 1, MinimumItemLevel = 10, MaximumItemLevel = 10,
            SuccessResult = MixResult.StaysAsIs, FailResult = MixResult.Disappear,
        });
        var chaos = new BasicModel.ItemCraftingRequiredItem { MinimumAmount = 1, MaximumAmount = 1, SuccessResult = MixResult.Disappear, FailResult = MixResult.Disappear };
        chaos.PossibleItems.Add(this._chaos);
        settings.RequiredItems.Add(chaos);
        if (acceptsTalisman)
        {
            var talisman = new BasicModel.ItemCraftingRequiredItem { MinimumAmount = 0, MaximumAmount = 1, SuccessResult = MixResult.Disappear, FailResult = MixResult.Disappear };
            talisman.PossibleItems.Add(this._talisman);
            settings.RequiredItems.Add(talisman);
        }

        settings.ResultItems.Add(new BasicModel.ItemCraftingResultItem { Reference = 1, AddLevel = 1 });
        return new GkzItemUpgradeCrafting(settings);
    }

    private async ValueTask<Player> CreatePlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.Configuration.Items.Add(this._sword);
        player.GameContext.Configuration.Items.Add(this._chaos);
        player.GameContext.Configuration.Items.Add(this._talisman);
        return player;
    }

    private async ValueTask<Item> AddAsync(Player player, ItemDefinition definition, byte slot, byte level = 0)
    {
        var item = new BasicModel.Item { Definition = definition, Level = level, Durability = definition.Durability };
        Assert.That(await player.TemporaryStorage!.AddItemAsync(slot, item).ConfigureAwait(false), Is.True);
        return item;
    }
}
