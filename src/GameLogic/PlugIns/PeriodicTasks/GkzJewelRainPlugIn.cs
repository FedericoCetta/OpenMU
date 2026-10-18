// <copyright file="GkzJewelRainPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using System.Runtime.InteropServices;
using System.Threading;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The jewel rain of MU GKZ: at the configured times, jewels fall on the ground around a point of a map
/// (by default the PvP ring of Lorencia), evenly spread over the duration of the event, each with fireworks.
/// Anybody can pick them up.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: lluvia de joyas", Description = "Every hour, jewels fall around the PvP ring of Lorencia for 2 minutes. Times, place, amount and jewels are in the configuration.")]
[Guid("3B7E9D21-4C58-4A6F-B2E0-7F1D5C8A9E63")]
public class GkzJewelRainPlugIn : PeriodicTaskBasePlugIn<GkzJewelRainConfiguration, GkzJewelRainPlugIn.RainState>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The distance up to which players see the fireworks of a falling jewel, in coordinates.
    /// </summary>
    private const int FireworksRange = 24;

    /// <inheritdoc />
    public object CreateDefaultConfig() => GkzJewelRainConfiguration.Default;

    /// <inheritdoc/>
    protected override RainState CreateState(IGameContext gameContext) => new(gameContext);

    /// <inheritdoc/>
    protected override ValueTask OnPrepareEventAsync(RainState state) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override async ValueTask OnPreparedAsync(RainState state)
    {
        if (this.Configuration is not { } configuration)
        {
            return;
        }

        // Out of the timetable, it was started by /lluvia: a shorter notice.
        if (!configuration.IsItTimeToStart(state.Context.ServerTimeZone))
        {
            state.NextRunUtc = DateTime.UtcNow.Add(configuration.ForcedPreStartMessageDelay);
            await SendMessageAsync(state.Context, configuration.ForcedStartMessage).ConfigureAwait(false);
            return;
        }

        await SendMessageAsync(state.Context, configuration.StartMessage).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async ValueTask OnStartedAsync(RainState state)
    {
        if (this.Configuration is not { } configuration)
        {
            return;
        }

        await SendMessageAsync(state.Context, configuration.RainStartedMessage).ConfigureAwait(false);

        if (state.Cancellation is { } previous)
        {
            await previous.CancelAsync().ConfigureAwait(false);
        }

        var cancellation = state.Cancellation = new CancellationTokenSource();
        _ = Task.Run(() => RainAsync(state.Context, configuration, cancellation.Token));
    }

    /// <inheritdoc/>
    protected override async ValueTask OnFinishedAsync(RainState state)
    {
        if (state.Cancellation is { } cancellation)
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            state.Cancellation = null;
        }

        await SendMessageAsync(state.Context, this.Configuration?.EndMessage).ConfigureAwait(false);
    }

    private static async Task RainAsync(IGameContext gameContext, GkzJewelRainConfiguration configuration, CancellationToken cancellationToken)
    {
        var logger = gameContext.LoggerFactory.CreateLogger<GkzJewelRainPlugIn>();
        try
        {
            var jewels = configuration.Jewels
                .Where(j => j.Weight > 0)
                .Select(j => (Definition: gameContext.Configuration.Items.FirstOrDefault(i => i.Group == j.Group && i.Number == j.Number), j.Weight))
                .Where(j => j.Definition is not null)
                .ToList();
            var totalWeight = jewels.Sum(j => j.Weight);
            if (totalWeight <= 0 || configuration.JewelCount <= 0
                || await gameContext.GetMapAsync((ushort)configuration.MapNumber).ConfigureAwait(false) is not { } map)
            {
                logger.LogWarning("Jewel rain: no jewels or no map {map} configured.", configuration.MapNumber);
                return;
            }

            var center = new Point(configuration.CenterX, configuration.CenterY);
            var interval = configuration.TaskDuration / configuration.JewelCount;
            for (var i = 0; i < configuration.JewelCount; i++)
            {
                var roll = Rand.NextInt(0, totalWeight);
                var definition = jewels.First(j => (roll -= j.Weight) < 0).Definition!;

                var item = new TemporaryItem
                {
                    Definition = definition,
                };
                item.Durability = item.IsStackable() ? 1 : definition.Durability;

                var position = map.Terrain.GetRandomCoordinate(center, configuration.Radius);
                await map.AddAsync(new DroppedItem(item, position, map, null, null)).ConfigureAwait(false);
                await ShowFireworksAsync(gameContext, map, position).ConfigureAwait(false);

                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The event finished before all jewels fell.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Jewel rain: unexpected error.");
        }
    }

    private static async ValueTask ShowFireworksAsync(IGameContext gameContext, GameMap map, Point position)
    {
        await gameContext.ForEachPlayerAsync(async player =>
        {
            if (player.CurrentMap != map
                || Math.Abs(player.Position.X - position.X) > FireworksRange
                || Math.Abs(player.Position.Y - position.Y) > FireworksRange)
            {
                return;
            }

            try
            {
                await player.InvokeViewPlugInAsync<IShowItemDropEffectPlugIn>(p => p.ShowEffectAsync(ItemDropEffect.Fireworks, position)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Jewel rain: unexpected error showing the fireworks.");
            }
        }).ConfigureAwait(false);
    }

    private static async ValueTask SendMessageAsync(IGameContext gameContext, LocalizedString? text)
    {
        if (text is not { } localized)
        {
            return;
        }

        await gameContext.ForEachPlayerAsync(async player =>
        {
            if (player.CurrentMap is null
                || player.PlayerState.CurrentState.IsDisconnectedOrFinished()
                || localized.GetTranslation(player.Culture) is not { Length: > 0 } message)
            {
                return;
            }

            try
            {
                await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.GoldenCenter)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                player.Logger.LogDebug(ex, "Jewel rain: unexpected error sending a message.");
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// The state of the jewel rain per game server.
    /// </summary>
    public sealed class RainState : PeriodicTaskGameServerState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RainState"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public RainState(IGameContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets the cancellation of the running rain.
        /// </summary>
        public CancellationTokenSource? Cancellation { get; set; }
    }
}
