// <copyright file="ItemPostTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.ItemPost;
using MUnique.OpenMU.GameLogic.Views.ItemPost;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for posting an item to the chat (Ctrl + right click) of MU GKZ.
/// </summary>
[TestFixture]
public class ItemPostTest
{
    private DateTime _now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tests that every player of the server sees the posted item.
    /// </summary>
    [Test]
    public async Task EveryPlayerSeesThePostAsync()
    {
        var (poster, item) = await this.CreatePlayerWithItemAsync().ConfigureAwait(false);
        var other = await PlayerTestHelper.CreatePlayerAsync(poster.GameContext).ConfigureAwait(false);
        await ((GameContext)poster.GameContext).AddPlayerAsync(poster).ConfigureAwait(false);
        await ((GameContext)poster.GameContext).AddPlayerAsync(other).ConfigureAwait(false);

        await this.CreateAction().PostAsync(poster, 12).ConfigureAwait(false);

        foreach (var player in new[] { poster, other })
        {
            Mock.Get(player.ViewPlugIns.GetPlugIn<IShowItemPostPlugIn>()!).Verify(v => v.ShowItemPostAsync(poster.Name, item), Times.Once);
        }
    }

    /// <summary>
    /// Tests that a player can post only once every 30 seconds.
    /// </summary>
    [Test]
    public async Task OnePostEveryThirtySecondsAsync()
    {
        var (poster, item) = await this.CreatePlayerWithItemAsync().ConfigureAwait(false);
        await ((GameContext)poster.GameContext).AddPlayerAsync(poster).ConfigureAwait(false);
        var action = this.CreateAction();
        var view = Mock.Get(poster.ViewPlugIns.GetPlugIn<IShowItemPostPlugIn>()!);

        await action.PostAsync(poster, 12).ConfigureAwait(false);
        this._now += TimeSpan.FromSeconds(29);
        await action.PostAsync(poster, 12).ConfigureAwait(false);
        view.Verify(v => v.ShowItemPostAsync(It.IsAny<string>(), item), Times.Once, "too early");

        this._now += TimeSpan.FromSeconds(1);
        await action.PostAsync(poster, 12).ConfigureAwait(false);
        view.Verify(v => v.ShowItemPostAsync(It.IsAny<string>(), item), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that an empty slot posts nothing and doesn't start the waiting time.
    /// </summary>
    [Test]
    public async Task EmptySlotPostsNothingAsync()
    {
        var (poster, item) = await this.CreatePlayerWithItemAsync().ConfigureAwait(false);
        await ((GameContext)poster.GameContext).AddPlayerAsync(poster).ConfigureAwait(false);
        var action = this.CreateAction();

        await action.PostAsync(poster, 13).ConfigureAwait(false);
        await action.PostAsync(poster, 12).ConfigureAwait(false);

        Mock.Get(poster.ViewPlugIns.GetPlugIn<IShowItemPostPlugIn>()!).Verify(v => v.ShowItemPostAsync(It.IsAny<string>(), item), Times.Once);
    }

    private ItemPostAction CreateAction() => new(() => this._now);

    private async ValueTask<(Player Player, Item Item)> CreatePlayerWithItemAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 8, Number = 1, Width = 2, Height = 2, Durability = 30 };
        definition.ItemSlot = new BasicModel.ItemSlotType();
        var item = new BasicModel.Item { Definition = definition, Durability = 30 };
        Assert.That(await player.Inventory!.AddItemAsync(12, item).ConfigureAwait(false), Is.True);
        return (player, item);
    }
}
