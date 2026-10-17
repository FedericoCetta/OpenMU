// <copyright file="GkzPublicWebController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.GameLogic.CastleSiege;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// The data of the public website of MU GKZ: server state, rankings, castle siege and events.
/// </summary>
/// <remarks>
/// It's anonymous on purpose, because the website is public; nginx forwards only these routes.
/// It's read-only: there are only GETs, and they take no text from the visitors (the ranking type
/// is checked against a fixed list). The answers contain only public data and are cached, so a
/// flood of requests doesn't reach the database.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/public-web")]
public class GkzPublicWebController : ControllerBase
{
    private const int CacheSeconds = 60;

    private static readonly Dictionary<string, string> EventNames = new()
    {
        [nameof(BloodCastleStartPlugIn)] = "Blood Castle",
        [nameof(ChaosCastleStartPlugIn)] = "Chaos Castle",
        [nameof(DevilSquareStartPlugIn)] = "Devil Square",
        [nameof(KanturuStartPlugIn)] = "Kanturu",
        [nameof(GoldenInvasionPlugIn)] = "Invasión dorada",
        [nameof(RedDragonInvasionPlugIn)] = "Red Dragon",
        [nameof(WhiteWizardInvasionPlugIn)] = "Invasión blanca",
        [nameof(HappyHourPlugIn)] = "Happy Hour",
    };

    private static readonly Dictionary<short, string> ClassNames = new()
    {
        [0] = "Dark Wizard", [2] = "Soul Master", [3] = "Grand Master",
        [4] = "Dark Knight", [6] = "Blade Knight", [7] = "Blade Master",
        [8] = "Fairy Elf", [10] = "Muse Elf", [11] = "High Elf",
        [12] = "Magic Gladiator", [13] = "Duel Master",
        [16] = "Dark Lord", [17] = "Lord Emperor",
    };

    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static readonly Dictionary<string, (DateTime ValidUntilUtc, object Value)> Cache = new();

    private readonly IDictionary<int, IGameServer> _gameServers;
    private readonly IGkzPublicWebQueries _queries;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzPublicWebController"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers.</param>
    /// <param name="queries">The queries.</param>
    public GkzPublicWebController(IDictionary<int, IGameServer> gameServers, IGkzPublicWebQueries queries)
    {
        this._gameServers = gameServers;
        this._queries = queries;
    }

    /// <summary>
    /// Gets if the server is online and how many real players are connected.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state.</returns>
    [HttpGet("status")]
    public Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
        => this.CachedAsync("status", this.CreateStatusAsync);

    /// <summary>
    /// Gets the state for the launcher, in the format of <c>api/status</c> but without the names of the players.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state.</returns>
    [HttpGet("launcher-status")]
    public async Task<IActionResult> GetLauncherStatusAsync(CancellationToken cancellationToken)
    {
        this.Response.Headers.CacheControl = "public, max-age=" + CacheSeconds;
        var status = (ServerStatus)await GetCachedAsync("status", this.CreateStatusAsync).ConfigureAwait(false);
        return this.Ok(new { state = status.Online ? "Online" : "Offline", players = status.Players });
    }

    /// <summary>
    /// Gets the next start of each event.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The events.</returns>
    [HttpGet("events")]
    public Task<IActionResult> GetEventsAsync(CancellationToken cancellationToken)
        => this.CachedAsync("events", () =>
        {
            var events = new List<object>();
            if (this.GetGameContext() is { } context)
            {
                foreach (var plugIn in context.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>())
                {
                    if (plugIn is IGkzScheduledEvent scheduled
                        && EventNames.TryGetValue(plugIn.GetType().Name, out var name)
                        && scheduled.GetGkzSchedule(context) is { } schedule
                        && (schedule.IsRunning || schedule.NextStartUtc is not null))
                    {
                        events.Add(new { name, next = schedule.NextStartUtc, active = schedule.IsRunning });
                    }
                }

                if (GetCastleSiege(context) is { } siege && siege.GetNextSiegeStartUtc() is { } siegeStart)
                {
                    events.Add(new { name = "Castle Siege", next = (DateTime?)siegeStart, active = siege.IsEventRunning });
                }
            }

            return Task.FromResult((object)new { now = DateTime.UtcNow, events });
        });

    /// <summary>
    /// Gets the owner of the castle and the next siege.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The castle.</returns>
    [HttpGet("castle")]
    public Task<IActionResult> GetCastleAsync(CancellationToken cancellationToken)
        => this.CachedAsync("castle", async () =>
        {
            var owner = await this._queries.GetCastleOwnerAsync(CancellationToken.None).ConfigureAwait(false);
            DateTime? nextSiege = null;
            if (this.GetGameContext() is { } context && GetCastleSiege(context) is { } siege)
            {
                nextSiege = siege.GetNextSiegeStartUtc();
            }

            return (object)new { owner = owner is null ? null : ToGuild(owner), nextSiege };
        });

    /// <summary>
    /// Gets a ranking.
    /// </summary>
    /// <param name="type">The type: resets, level, killers or guilds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The best 50.</returns>
    [HttpGet("rankings/{type}")]
    public Task<IActionResult> GetRankingAsync(string type, CancellationToken cancellationToken)
    {
        GkzCharacterRankingOrder order;
        switch (type)
        {
            case "resets": order = GkzCharacterRankingOrder.Resets; break;
            case "level": order = GkzCharacterRankingOrder.Level; break;
            case "killers": order = GkzCharacterRankingOrder.Kills; break;
            case "guilds":
                return this.CachedAsync("rankings/guilds", async () =>
                {
                    var guilds = await this._queries.GetGuildRankingAsync(CancellationToken.None).ConfigureAwait(false);
                    return (object)new { updated = DateTime.UtcNow, entries = guilds.Select(ToGuild).ToList() };
                });
            default:
                return Task.FromResult<IActionResult>(this.NotFound());
        }

        return this.CachedAsync("rankings/" + type, async () =>
        {
            var characters = await this._queries.GetCharacterRankingAsync(order, CancellationToken.None).ConfigureAwait(false);
            var entries = characters.Select(c => new
            {
                name = c.Name,
                @class = ClassNames.GetValueOrDefault(c.ClassNumber, string.Empty),
                level = c.Level,
                resets = c.Resets,
                kills = c.Kills,
                guild = c.Guild,
            }).ToList();
            return (object)new { updated = DateTime.UtcNow, entries };
        });
    }

    private async Task<object> CreateStatusAsync()
    {
        var players = 0;
        var online = false;
        foreach (var server in this._gameServers.Values.OfType<GameServer>())
        {
            if (server.ServerState != ServerState.Started)
            {
                continue;
            }

            online = true;
            await server.Context.ForEachPlayerAsync(player =>
            {
                if (player is not OfflinePlayer && player.Account is { IsBot: false })
                {
                    Interlocked.Increment(ref players);
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        return new ServerStatus(online, players);
    }

    private static object ToGuild(GkzRankedGuild guild) => new
    {
        name = guild.Name,
        master = guild.Master,
        members = guild.Members,
        score = guild.Score,
        mark = guild.Logo is { Length: 32 } logo ? Convert.ToHexString(logo) : null,
    };

    private static CastleSiegeContext? GetCastleSiege(GameLogic.IGameContext context)
        => context.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<CastleSiegePlugIn>().FirstOrDefault()?.GetContext(context);

    private GameLogic.IGameContext? GetGameContext()
        => this._gameServers.Values.OfType<GameServer>().FirstOrDefault(s => s.ServerState == ServerState.Started)?.Context;

    private async Task<IActionResult> CachedAsync(string key, Func<Task<object>> create)
    {
        this.Response.Headers.CacheControl = "public, max-age=" + CacheSeconds;
        return this.Ok(await GetCachedAsync(key, create).ConfigureAwait(false));
    }

    /// <remarks>
    /// The answer is created without the cancellation of the request, because it's shared with the other visitors.
    /// </remarks>
    private static async Task<object> GetCachedAsync(string key, Func<Task<object>> create)
    {
        var now = DateTime.UtcNow;
        await CacheLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!Cache.TryGetValue(key, out var entry) || entry.ValidUntilUtc <= now)
            {
                entry = (now.AddSeconds(CacheSeconds), await create().ConfigureAwait(false));
                Cache[key] = entry;
            }

            return entry.Value;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    /// <summary>
    /// If the server is online and how many real players are connected.
    /// </summary>
    /// <param name="Online">If the server is online.</param>
    /// <param name="Players">The number of real players.</param>
    private sealed record ServerStatus(bool Online, int Players);
}
