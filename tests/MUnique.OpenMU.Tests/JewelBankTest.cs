// <copyright file="JewelBankTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views.JewelBank;
using MUnique.OpenMU.GameServer.RemoteView.Gkz;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the jewel bank of MU GKZ.
/// </summary>
[TestFixture]
public class JewelBankTest
{
    private const byte JewelGroup = 14;
    private const short BlessNumber = 13;

    private readonly JewelBankActions _actions = new();

    /// <summary>
    /// Tests that a deposit moves all pieces of the item from the inventory into the bank, stacks and single ones.
    /// </summary>
    [Test]
    public async Task DepositMovesAllPiecesAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        await AddAsync(player, bless, 12, 30).ConfigureAwait(false);
        await AddAsync(player, bless, 13, 1).ConfigureAwait(false);

        await this._actions.DepositAsync(player, JewelGroup, BlessNumber, 0).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count(i => i.Definition == bless), Is.Zero);
        Assert.That(player.Account!.BankItems.Single().Count, Is.EqualTo(31));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowJewelBankPlugIn>()!)
            .Verify(v => v.ShowJewelBankAsync(It.Is<IReadOnlyList<(JewelBankEntry Entry, int Count)>>(l => l.Single(e => e.Entry.Number == BlessNumber).Count == 31), false), Times.Once);
    }

    /// <summary>
    /// Tests that a deposit of some pieces takes them from the smallest stacks first, and only a part of a stack.
    /// </summary>
    [Test]
    public async Task DepositSomePiecesAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        var big = await AddAsync(player, bless, 12, 30).ConfigureAwait(false);
        await AddAsync(player, bless, 13, 4).ConfigureAwait(false);

        await this._actions.DepositAsync(player, JewelGroup, BlessNumber, 0, 10).ConfigureAwait(false);

        Assert.That(player.Inventory!.GetItem(13), Is.Null, "the small stack went first");
        Assert.That(big.Durability, Is.EqualTo(24));
        Assert.That(player.Account!.BankItems.Single().Count, Is.EqualTo(10));
    }

    /// <summary>
    /// Tests that a withdrawal fills the stack in the inventory first, then makes new stacks of at most 50.
    /// </summary>
    [Test]
    public async Task WithdrawFillsStacksFirstAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        var stack = await AddAsync(player, bless, 12, 45).ConfigureAwait(false);
        AddToBank(player, 100);

        await this._actions.WithdrawAsync(player, JewelGroup, BlessNumber, 0, 60).ConfigureAwait(false);

        Assert.That(stack.Durability, Is.EqualTo(50));
        var newStacks = player.Inventory!.Items.Where(i => i.Definition == bless && i != stack).Select(i => i.Durability).OrderBy(d => d);
        Assert.That(newStacks, Is.EqualTo(new double[] { 5, 50 }), "55 more pieces: a full stack and the rest");
        Assert.That(player.Account!.BankItems.Single().Count, Is.EqualTo(40));
    }

    /// <summary>
    /// Tests that the bank doesn't give more than it has.
    /// </summary>
    [Test]
    public async Task WithdrawAtMostTheBankAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        AddToBank(player, 3);

        await this._actions.WithdrawAsync(player, JewelGroup, BlessNumber, 0, 10).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Single(i => i.Definition == bless).Durability, Is.EqualTo(3));
        Assert.That(player.Account!.BankItems.Single().Count, Is.Zero);
    }

    /// <summary>
    /// Tests that items with options don't go into the bank, which only counts items.
    /// </summary>
    [Test]
    public async Task ItemsWithOptionsStayInTheInventoryAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        var item = await AddAsync(player, bless, 12, 1).ConfigureAwait(false);
        item.HasSkill = true;

        await this._actions.DepositAsync(player, JewelGroup, BlessNumber, 0).ConfigureAwait(false);

        Assert.That(player.Inventory!.GetItem(12), Is.SameAs(item));
        Assert.That(player.Account!.BankItems, Is.Empty);
    }

    /// <summary>
    /// Tests the layout of the bank packet, which the MU GKZ client reads (GkzPackets.h).
    /// </summary>
    [Test]
    public void PacketLayout()
    {
        var packet = new byte[ShowJewelBankPlugIn.HeaderSize + (2 * ShowJewelBankPlugIn.EntrySize)];
        var length = ShowJewelBankPlugIn.Write(packet, [(new JewelBankEntry(14, 13, 0), 78), (new JewelBankEntry(14, 11, 10), 2)], true);

        Assert.That(length, Is.EqualTo(packet.Length));
        Assert.That(packet[..6], Is.EqualTo(new byte[] { 0xC1, (byte)length, 0xEC, 0x31, 1, 2 }));
        Assert.That(packet[6..14], Is.EqualTo(new byte[] { 14, 13, 0, 0, 78, 0, 0, 0 }));
        Assert.That(packet[14..22], Is.EqualTo(new byte[] { 14, 11, 0, 10, 2, 0, 0, 0 }));
    }

    private static void AddToBank(Player player, int count)
    {
        player.Account!.BankItems.Add(new BasicModel.AccountBankItem { ItemGroup = JewelGroup, ItemNumber = BlessNumber, Count = count });
    }

    private static async ValueTask<Item> AddAsync(Player player, ItemDefinition definition, byte slot, byte pieces)
    {
        var item = new BasicModel.Item { Definition = definition, Durability = pieces };
        Assert.That(await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false), Is.True);
        return item;
    }

    private static async ValueTask<(Player Player, ItemDefinition Bless)> CreatePlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        Mock.Get(player.Account!).Setup(a => a.BankItems).Returns(new List<AccountBankItem>());
        var bless = new BasicModel.ItemDefinition { Group = JewelGroup, Number = BlessNumber, Width = 1, Height = 1, Durability = 50 };
        player.GameContext.Configuration.Items.Add(bless);
        player.GameContext.FeaturePlugIns.AddPlugIn(new JewelBankPlugIn { Configuration = new JewelBankConfiguration() }, true);
        return (player, bless);
    }
}
