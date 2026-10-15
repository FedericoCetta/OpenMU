// <copyright file="ItemPostAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemPost;

using System.Collections.Concurrent;
using MUnique.OpenMU.GameLogic.Views.ItemPost;
using MUnique.OpenMU.Persistence;

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

    /// <summary>
    /// The time of the last post of each character of the server. It's kept by character, not by
    /// connection, so a reconnect doesn't reset the cooldown.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, DateTime> ServerLastPosts = new();

    private readonly Func<DateTime> _now;

    private readonly ConcurrentDictionary<Guid, DateTime> _lastPosts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPostAction"/> class.
    /// </summary>
    public ItemPostAction()
        : this(() => DateTime.UtcNow, ServerLastPosts)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPostAction"/> class.
    /// </summary>
    /// <param name="now">Gets the current time.</param>
    /// <param name="lastPosts">The time of the last post of each character; by default an own one (tests).</param>
    public ItemPostAction(Func<DateTime> now, ConcurrentDictionary<Guid, DateTime>? lastPosts = null)
    {
        this._now = now;
        this._lastPosts = lastPosts ?? new ConcurrentDictionary<Guid, DateTime>();
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
        if (player.Account?.ChatBanUntil is { } chatBanUntil && chatBanUntil > now)
        {
            await player.ShowBlueMessageAsync("No podés publicar items mientras tenés el chat bloqueado.").ConfigureAwait(false);
            return;
        }

        var characterId = player.SelectedCharacter.GetId();
        var wait = this._lastPosts.GetValueOrDefault(characterId) + Cooldown - now;
        if (wait > TimeSpan.Zero)
        {
            await player.ShowBlueMessageAsync($"Podés publicar otro item en {Math.Ceiling(wait.TotalSeconds)} segundos.").ConfigureAwait(false);
            return;
        }

        this._lastPosts[characterId] = now;
        var sender = player.Name;
        player.Logger.LogInformation("Player {player} posted the item {item}.", player, item);
        await player.GameContext.ForEachPlayerAsync(p => p.InvokeViewPlugInAsync<IShowItemPostPlugIn>(v => v.ShowItemPostAsync(sender, item)).AsTask()).ConfigureAwait(false);
    }
}
