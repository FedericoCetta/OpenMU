// <copyright file="SplitStackTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for taking one piece off a stack (Ctrl + click) of MU GKZ.
/// </summary>
[TestFixture]
public class SplitStackTest
{
    private readonly SplitStackAction _action = new();

    /// <summary>
    /// Tests that one piece leaves the stack and lands in a free slot as its own item.
    /// </summary>
    [Test]
    public async Task TakesOnePieceOffAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        var stack = await AddAsync(player, bless, 12, 8).ConfigureAwait(false);

        await this._action.SplitAsync(player, 12).ConfigureAwait(false);

        Assert.That(stack.Durability, Is.EqualTo(7));
        var piece = player.Inventory!.Items.Single(i => i.Definition == bless && i != stack);
        Assert.That(piece.Durability, Is.EqualTo(1));
        Assert.That(piece.ItemSlot, Is.Not.EqualTo(12));
    }

    /// <summary>
    /// Tests that a single piece isn't split.
    /// </summary>
    [Test]
    public async Task SinglePieceStaysAsync()
    {
        var (player, bless) = await CreatePlayerAsync().ConfigureAwait(false);
        await AddAsync(player, bless, 12, 1).ConfigureAwait(false);

        await this._action.SplitAsync(player, 12).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count(i => i.Definition == bless), Is.EqualTo(1));
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
        var bless = new BasicModel.ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1, Durability = 50 };
        player.GameContext.Configuration.Items.Add(bless);
        return (player, bless);
    }
}
