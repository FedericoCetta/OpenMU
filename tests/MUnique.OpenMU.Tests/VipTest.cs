// <copyright file="VipTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views.Vip;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.GameServer.RemoteView.Gkz;

/// <summary>
/// Tests for the VIP of MU GKZ: the purchase, the bonuses and the vaults.
/// </summary>
[TestFixture]
public class VipTest
{
    private readonly VipActions _actions = new();

    /// <summary>
    /// Tests that the VIP is bought with enough coins: the coins are taken and the bonuses are added.
    /// </summary>
    [Test]
    public async Task BuyWithEnoughCoinsAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 1500;

        await this._actions.BuyAsync(player).ConfigureAwait(false);

        Assert.That(player.Account.WCoinC, Is.EqualTo(500));
        Assert.That(player.Account.VipUntil, Is.EqualTo(DateTime.UtcNow.AddDays(30)).Within(TimeSpan.FromMinutes(1)));
        GetView(player).Verify(v => v.ShowVipBuyResultAsync(VipBuyResult.Success), Times.Once);
        var attributes = player.Attributes!;
        Assert.That(attributes[Stats.ExperienceRate], Is.EqualTo(1.45f).Within(0.001f));
        Assert.That(attributes[Stats.MoneyAmountRate], Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(attributes[Stats.ItemDropChanceBonus], Is.EqualTo(0.05f).Within(0.0001f));
        Assert.That(attributes[Stats.IsVip], Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the VIP isn't bought without enough coins.
    /// </summary>
    [Test]
    public async Task BuyWithoutEnoughCoinsAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 999;

        await this._actions.BuyAsync(player).ConfigureAwait(false);

        Assert.That(player.Account.WCoinC, Is.EqualTo(999));
        Assert.That(player.Account.VipUntil, Is.Null);
        GetView(player).Verify(v => v.ShowVipBuyResultAsync(VipBuyResult.NotEnoughCoins), Times.Once);
        Assert.That(player.Attributes![Stats.ExperienceRate], Is.EqualTo(1));
        Assert.That(player.Attributes[Stats.IsVip], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the VIP can't be bought again while it's active: the periods don't add up.
    /// </summary>
    [Test]
    public async Task BuyWhileVipIsRefusedAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        var end = DateTime.UtcNow.AddDays(10);
        player.Account!.VipUntil = end;
        player.Account.WCoinC = 1000;

        await this._actions.BuyAsync(player).ConfigureAwait(false);

        GetView(player).Verify(v => v.ShowVipBuyResultAsync(VipBuyResult.AlreadyVip), Times.Once);
        Assert.That(player.Account.VipUntil, Is.EqualTo(end));
        Assert.That(player.Account.WCoinC, Is.EqualTo(1000));
    }

    /// <summary>
    /// Tests that the VIP can be bought again when it ended, from now.
    /// </summary>
    [Test]
    public async Task BuyAfterVipEndedAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.VipUntil = DateTime.UtcNow.AddDays(-3);
        player.Account.WCoinC = 1000;

        await this._actions.BuyAsync(player).ConfigureAwait(false);

        Assert.That(player.Account.VipUntil, Is.EqualTo(DateTime.UtcNow.AddDays(30)).Within(TimeSpan.FromMinutes(1)));
        Assert.That(player.Account.WCoinC, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the bonuses are removed when the VIP ends, and that they're added only once.
    /// </summary>
    [Test]
    public async Task BonusesFollowTheVipStateAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.VipUntil = DateTime.UtcNow.AddMinutes(5);

        Assert.That(VipPlugIn.UpdateBonuses(player), Is.False);
        Assert.That(VipPlugIn.UpdateBonuses(player), Is.False);
        Assert.That(player.Attributes![Stats.ExperienceRate], Is.EqualTo(1.45f).Within(0.001f));

        player.Account.VipUntil = DateTime.UtcNow.AddMinutes(-1);
        Assert.That(VipPlugIn.UpdateBonuses(player), Is.True);
        Assert.That(player.Attributes[Stats.ExperienceRate], Is.EqualTo(1));
        Assert.That(player.Attributes[Stats.MoneyAmountRate], Is.EqualTo(1));
        Assert.That(player.Attributes[Stats.ItemDropChanceBonus], Is.EqualTo(0));
        Assert.That(player.Attributes[Stats.IsVip], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests the number of vaults: 2 without VIP, 6 with VIP, 1 without the VIP plugin.
    /// </summary>
    [Test]
    public async Task AvailableVaultsAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        var account = player.Account!;
        Assert.That(account.GetAvailableVaults(player.GameContext), Is.EqualTo(2));

        account.VipUntil = DateTime.UtcNow.AddDays(1);
        Assert.That(account.GetAvailableVaults(player.GameContext), Is.EqualTo(6));

        var withoutVip = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        Assert.That(withoutVip.Account!.GetAvailableVaults(withoutVip.GameContext), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that a VIP vault can only be opened while VIP, and that its items are kept when the VIP ends.
    /// </summary>
    [Test]
    public async Task VipVaultsAreKeptButHiddenAfterTheVipAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        var account = player.Account!;
        await OpenVaultAsync(player).ConfigureAwait(false);
        Assert.That(player.Vault!.ItemStorage, Is.SameAs(account.Vault));
        GetView(player).Verify(v => v.ShowVaultSelectionAsync(1, 2), Times.Once);

        await this._actions.SelectVaultAsync(player, 3).ConfigureAwait(false);
        Assert.That(player.Vault!.ItemStorage, Is.SameAs(account.Vault), "vault 3 needs VIP");

        account.VipUntil = DateTime.UtcNow.AddDays(1);
        await this._actions.SelectVaultAsync(player, 3).ConfigureAwait(false);
        Assert.That(player.Vault!.ItemStorage, Is.SameAs(account.Vault3));
        GetView(player).Verify(v => v.ShowVaultSelectionAsync(3, 6), Times.Once);

        // Switching keeps the window open: reopening it would make the client close the vault.
        Mock.Get(player.ViewPlugIns.GetPlugIn<GameLogic.Views.Vault.IShowVaultPlugIn>()!).Verify(v => v.ShowVaultAsync(), Times.Once);
        Mock.Get(player.ViewPlugIns.GetPlugIn<GameLogic.Views.NPC.IShowMerchantStoreItemListPlugIn>()!)
            .Verify(v => v.ShowMerchantStoreItemListAsync(account.Vault3!.Items, GameLogic.Views.NPC.StoreKind.Normal), Times.Once);
        var vault3 = account.Vault3!;
        vault3.Money = 1234;

        account.VipUntil = DateTime.UtcNow.AddDays(-1);
        await this._actions.SelectVaultAsync(player, 2).ConfigureAwait(false);
        Assert.That(player.Vault!.ItemStorage, Is.SameAs(account.Vault2));
        await this._actions.SelectVaultAsync(player, 3).ConfigureAwait(false);
        Assert.That(player.Vault!.ItemStorage, Is.SameAs(account.Vault2));
        Assert.That(account.Vault3, Is.SameAs(vault3));
        Assert.That(vault3.Money, Is.EqualTo(1234));
    }

    /// <summary>
    /// Tests that the vault can't be switched without an opened vault.
    /// </summary>
    [Test]
    public async Task VaultSelectionNeedsOpenedVaultAsync()
    {
        var player = await CreatePlayerAsync().ConfigureAwait(false);
        await this._actions.SelectVaultAsync(player, 2).ConfigureAwait(false);
        Assert.That(player.Vault, Is.Null);
        Assert.That(player.Account!.Vault2, Is.Null);
    }

    /// <summary>
    /// Tests the layout of the VIP info packet, which the MU GKZ client reads (GkzPackets.h).
    /// </summary>
    [Test]
    public void VipInfoPacketLayout()
    {
        var now = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
        var info = new VipInfo(2500, now.AddHours(1), new VipConfiguration());
        var packet = new byte[ShowVipPlugIn.VipInfoLength];

        ShowVipPlugIn.WriteVipInfo(packet, info, now);

        Assert.That(packet[..4], Is.EqualTo(new byte[] { 0xC1, 42, 0xEC, 0x01 }));
        Assert.That(BitConverter.ToUInt32(packet, 4), Is.EqualTo(2500));
        Assert.That(BitConverter.ToUInt32(packet, 8), Is.EqualTo(3600));
        Assert.That(BitConverter.ToUInt32(packet, 12), Is.EqualTo(1000));
        Assert.That(BitConverter.ToUInt16(packet, 16), Is.EqualTo(30));
        Assert.That(BitConverter.ToUInt16(packet, 18), Is.EqualTo(45));
        Assert.That(BitConverter.ToUInt16(packet, 20), Is.EqualTo(50));
        Assert.That(BitConverter.ToUInt16(packet, 22), Is.EqualTo(50));
        Assert.That(packet[24], Is.EqualTo(2));
        Assert.That(packet[25], Is.EqualTo(6));
        Assert.That(System.Text.Encoding.ASCII.GetString(packet, 26, 16).TrimEnd('\0'), Is.EqualTo("GKZ Coins"));
    }

    /// <summary>
    /// Tests that the item drop chance bonus is spread over the item groups and leaves the money drop alone.
    /// </summary>
    [Test]
    public void ItemDropChanceBonusGoesToItemGroups()
    {
        var groups = new List<DropItemGroup>
        {
            new() { Chance = 0.5, ItemType = SpecialItemType.Money },
            new() { Chance = 0.3, ItemType = SpecialItemType.RandomItem },
            new() { Chance = 0.001, ItemType = SpecialItemType.Jewel },
        };

        var chances = DefaultDropGenerator.GetChances(groups, 0.05);

        Assert.That(chances[0], Is.EqualTo(0.5));
        Assert.That(chances[1] + chances[2], Is.EqualTo(0.351).Within(1e-9), "5 percentage points more item drops");
        Assert.That(chances[1] / chances[2], Is.EqualTo(300).Within(1e-6), "in proportion to the groups");
        Assert.That(DefaultDropGenerator.GetChances(groups, 0), Is.EqualTo(new[] { 0.5, 0.3, 0.001 }));
    }

    private static Mock<IShowVipPlugIn> GetView(Player player)
    {
        return Mock.Get(player.ViewPlugIns.GetPlugIn<IShowVipPlugIn>()!);
    }

    private static async ValueTask OpenVaultAsync(Player player)
    {
        player.OpenedNpc = new NonPlayerCharacter(null!, new MonsterDefinition { NpcWindow = NpcWindow.VaultStorage }, null!);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.NpcDialogOpened).ConfigureAwait(false);
        await VipActions.OpenVaultAsync(player).ConfigureAwait(false);
    }

    private static async ValueTask<Player> CreatePlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var account = Mock.Get(player.Account!);
        account.SetupProperty(a => a.Vault);
        account.SetupProperty(a => a.Vault2);
        account.SetupProperty(a => a.Vault3);
        account.SetupProperty(a => a.Vault4);
        account.SetupProperty(a => a.Vault5);
        account.SetupProperty(a => a.Vault6);
        player.GameContext.FeaturePlugIns.AddPlugIn(new VipPlugIn { Configuration = new VipConfiguration() }, true);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);

        // The base values which the game configuration gives these rates.
        foreach (var rate in new[] { Stats.ExperienceRate, Stats.MasterExperienceRate, Stats.MoneyAmountRate })
        {
            if (player.Attributes![rate] == 0)
            {
                player.Attributes.AddElement(new ConstantElement(1), rate);
            }
        }

        return player;
    }
}
