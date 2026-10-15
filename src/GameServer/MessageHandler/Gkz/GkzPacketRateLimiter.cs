// <copyright file="GkzPacketRateLimiter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;

/// <summary>
/// Limits how many packets of the MU GKZ client (0xEC) a player may send, so a forged client can't load the
/// database or stall the market for everybody by flooding requests. It's a token bucket per player: the
/// requests which do database or market work have a lower rate than the others.
/// </summary>
internal sealed class GkzPacketRateLimiter
{
    /// <summary>
    /// The sub codes of the requests which query the database or scan the market.
    /// </summary>
    private static readonly HashSet<byte> ExpensiveSubCodes =
    [
        0x01, // VIP info: applies pending coin grants (database)
        0x20, // item combination check (database)
        0x30, // jewel bank request
        0x61, // market page: renews the stores and scans all listings
        0x62, // market store
    ];

    private readonly ConditionalWeakTable<Player, State> _states = new();

    private readonly Func<DateTime> _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzPacketRateLimiter"/> class.
    /// </summary>
    /// <param name="now">Gets the current time.</param>
    public GkzPacketRateLimiter(Func<DateTime>? now = null)
    {
        this._now = now ?? (() => DateTime.UtcNow);
    }

    /// <summary>
    /// Determines whether the player may send a packet with the sub code now; if so, it's counted.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="subCode">The sub code of the packet.</param>
    /// <returns><c>true</c>, if the packet may be handled.</returns>
    public bool TryAcquire(Player player, byte subCode)
    {
        var expensive = ExpensiveSubCodes.Contains(subCode);
        var state = this._states.GetValue(player, _ => new State(this._now()));
        bool allowed;
        bool log;
        lock (state)
        {
            var now = this._now();
            var elapsed = Math.Max(0, (now - state.LastUpdate).TotalSeconds);
            state.LastUpdate = now;
            state.Tokens = Math.Min(Limits.Capacity, state.Tokens + (elapsed * Limits.PerSecond));
            state.ExpensiveTokens = Math.Min(Limits.ExpensiveCapacity, state.ExpensiveTokens + (elapsed * Limits.ExpensivePerSecond));

            allowed = state.Tokens >= 1 && (!expensive || state.ExpensiveTokens >= 1);
            if (allowed)
            {
                state.Tokens -= 1;
                if (expensive)
                {
                    state.ExpensiveTokens -= 1;
                }
            }

            log = !allowed && now - state.LastLog > TimeSpan.FromSeconds(10);
            if (log)
            {
                state.LastLog = now;
            }
        }

        if (log)
        {
            player.Logger.LogWarning("Player {player} sends too many MU GKZ packets (sub code 0x{subCode:X2}); dropping some.", player, subCode);
        }

        return allowed;
    }

    /// <summary>
    /// The limits. A normal client stays far below them, e.g. scrolling through the market pages.
    /// </summary>
    internal static class Limits
    {
        /// <summary>The burst of packets.</summary>
        public const double Capacity = 40;

        /// <summary>The packets per second.</summary>
        public const double PerSecond = 20;

        /// <summary>The burst of expensive requests.</summary>
        public const double ExpensiveCapacity = 8;

        /// <summary>The expensive requests per second.</summary>
        public const double ExpensivePerSecond = 4;
    }

    private sealed class State
    {
        public State(DateTime now)
        {
            this.LastUpdate = now;
        }

        public double Tokens { get; set; } = Limits.Capacity;

        public double ExpensiveTokens { get; set; } = Limits.ExpensiveCapacity;

        public DateTime LastUpdate { get; set; }

        public DateTime LastLog { get; set; } = DateTime.MinValue;
    }
}
