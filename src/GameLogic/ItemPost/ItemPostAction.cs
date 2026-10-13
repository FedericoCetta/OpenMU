// <copyright file="ItemPostAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemPost;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.Views.ItemPost;

/// <summary>
/// MU GKZ: posts an item of the inventory to the chat of every player of the server (Ctrl + right click),
/// e.g. to sell it. The other players see the item exactly as it is when they point at the message.
/// </summary>
public class ItemPostAction
{
    /// <summary>
    /// The time a player has to wait between two posts.
    /// </summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    private static readonly ConditionalWeakTable<Player, StrongBox<DateTime>> LastPosts = new();

    private readonly Func<DateTime> _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPostAction"/> class.
    /// </summary>
    public ItemPostAction()
        : this(() => DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPostAction"/> class.
    /// </summary>
    /// <param name="now">Gets the current time.</param>
    public ItemPostAction(Func<DateTime> now)
    {
        this._now = now;
    }

    /// <summary>
    /// Posts the item at the inventory slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The inventory slot.</param>
    public async ValueTask PostAsync(Player player, byte slot)
    {
        if (player.Inventory?.GetItem(slot) is not { Definition: not null } item || player.SelectedCharacter is null)
        {
            return;
        }

        var now = this._now();
        var lastPost = LastPosts.GetOrCreateValue(player);
        var wait = lastPost.Value + Cooldown - now;
        if (wait > TimeSpan.Zero)
        {
            await player.ShowBlueMessageAsync($"Podés publicar otro item en {Math.Ceiling(wait.TotalSeconds)} segundos.").ConfigureAwait(false);
            return;
        }

        lastPost.Value = now;
        var sender = player.Name;
        player.Logger.LogInformation("Player {player} posted the item {item}.", player, item);
        await player.GameContext.ForEachPlayerAsync(p => p.InvokeViewPlugInAsync<IShowItemPostPlugIn>(v => v.ShowItemPostAsync(sender, item)).AsTask()).ConfigureAwait(false);
    }
}
