// <copyright file="GkzGuardPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.AntiCheat;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The anti-cheat of the MU GKZ client, on the server: when a player enters the game, the server sends a
/// challenge, and the client answers every 10 seconds with a heartbeat which is signed with it. The heartbeat
/// tells what the client detected (debuggers, Cheat Engine, changed code ...) and its clock, which runs ahead
/// of the server clock with a speedhack. A client without heartbeats (a bot, the original client) is
/// disconnected, as well as one which detected something.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: anti-cheat", Description = "Checks the heartbeats of the anti-cheat of the MU GKZ client: detections, speedhack by the clock, and missing heartbeats.")]
[Guid("5C2E8F14-9A3B-4D71-B6E0-2F8A1C7D9E43")]
public class GkzGuardPlugIn : IFeaturePlugIn, IPeriodicTaskPlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<GkzGuardConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The programs which the client reports by their index (<see cref="GkzGuardFlags.ForbiddenProgram"/>);
    /// the same list is in MuMain src/source/Security/GkzGuard.cpp.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenProgramNames =
    [
        "Cheat Engine", "ArtMoney", "WPE Pro", "RPE", "Packet Editor", "x64dbg", "x32dbg", "OllyDbg", "IDA", "Speed Gear", "Speedhack",
    ];

    /// <summary>
    /// The heartbeats which are used to find the offset of the client clock; the smallest one is the reference.
    /// </summary>
    private const int ReferenceHeartbeats = 6;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private readonly ConditionalWeakTable<Player, GuardState> _states = new();

    private readonly ConditionalWeakTable<GameContext, StrongBox<DateTime>> _nextChecks = new();

    private readonly Func<DateTime> _now;

    private GkzGuardConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzGuardPlugIn"/> class.
    /// </summary>
    public GkzGuardPlugIn()
        : this(() => DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzGuardPlugIn"/> class.
    /// </summary>
    /// <param name="now">Gets the current time.</param>
    public GkzGuardPlugIn(Func<DateTime> now)
    {
        this._now = now;
    }

    /// <inheritdoc />
    public GkzGuardConfiguration? Configuration
    {
        get => this._configuration;
        set
        {
            this._configuration = value;
            GkzSecurityIncidents.DiscordWebhookUrl = value?.DiscordWebhookUrl;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GkzGuardConfiguration();

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld || IsServerControlled(player) || this._states.TryGetValue(player, out _))
        {
            return;
        }

        // The first time the player enters the game on this connection: send the challenge.
        var state = new GuardState(BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4)), this._now());
        this._states.AddOrUpdate(player, state);
        await player.InvokeViewPlugInAsync<IShowGkzGuardChallengePlugIn>(p => p.ShowChallengeAsync(state.Challenge)).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles a heartbeat of the client.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sequence">The sequence number.</param>
    /// <param name="clientTime">The time of the client, in milliseconds since its anti-cheat started.</param>
    /// <param name="flags">The detections.</param>
    /// <param name="detail">The detail of the detections.</param>
    /// <param name="proof">The proof.</param>
    /// <returns>The task.</returns>
    public async ValueTask HandleHeartbeatAsync(Player player, uint sequence, uint clientTime, uint flags, uint detail, uint proof)
    {
        if (!this._states.TryGetValue(player, out var state))
        {
            return;
        }

        var configuration = this.Configuration ??= new GkzGuardConfiguration();
        var now = this._now();
        string? violation = null;
        var ban = false;
        lock (state)
        {
            if (proof != GkzGuardProof.Compute(GkzGuardProof.Secret, state.Challenge, sequence, clientTime, flags, detail))
            {
                violation = "a forged heartbeat";
                ban = true;
            }
            else if (sequence <= state.LastSequence && state.LastSequence != 0)
            {
                violation = "a repeated heartbeat";
                ban = true;
            }
            else
            {
                state.LastSequence = sequence;
                state.LastHeartbeat = now;
                violation = this.CheckClock(state, now, clientTime, configuration, out ban) ?? DescribeDetections((GkzGuardFlags)flags, detail, ref ban);
            }
        }

        if (violation is not null)
        {
            await this.HandleViolationAsync(player, violation, ban, configuration).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var now = this._now();
        var nextCheck = this._nextChecks.GetValue(gameContext, _ => new StrongBox<DateTime>(now));
        if (now < nextCheck.Value)
        {
            return;
        }

        nextCheck.Value = now + CheckInterval;
        var configuration = this.Configuration ??= new GkzGuardConfiguration();
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.PlayerState.CurrentState == PlayerState.Disconnected
                || IsServerControlled(player)
                || !this._states.TryGetValue(player, out var state))
            {
                continue;
            }

            if (now - state.LastHeartbeat > configuration.HeartbeatTimeout)
            {
                // Once is enough; the player is disconnected.
                state.LastHeartbeat = DateTime.MaxValue;
                await this.HandleViolationAsync(player, "no heartbeats", false, configuration).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void ForceStart()
    {
    }

    /// <summary>
    /// Gets the last violation of the player (diagnostics and tests).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The last violation, or <c>null</c>.</returns>
    public string? GetLastViolation(Player player)
    {
        return this._states.TryGetValue(player, out var state) ? state.LastViolation : null;
    }

    private static bool IsServerControlled(Player player) => player is OfflinePlayer;

    private static string? DescribeDetections(GkzGuardFlags flags, uint detail, ref bool ban)
    {
        if (flags == GkzGuardFlags.None)
        {
            return null;
        }

        // Debuggers and changed code mean that somebody works on the client; a program may be open by mistake.
        ban = flags.HasFlag(GkzGuardFlags.CodeModified) || flags.HasFlag(GkzGuardFlags.ForbiddenModule);
        var program = flags.HasFlag(GkzGuardFlags.ForbiddenProgram) && detail < ForbiddenProgramNames.Count
            ? $" ({ForbiddenProgramNames[(int)detail]})"
            : string.Empty;
        return $"{flags}{program}";
    }

    private string? CheckClock(GuardState state, DateTime now, uint clientTime, GkzGuardConfiguration configuration, out bool ban)
    {
        ban = false;

        // The offset between the server clock and the client clock. Network delays only make it bigger, so the
        // smallest one of the first heartbeats is the reference. A speedhack makes the client clock run faster,
        // so the offset gets smaller over time: the client clock is ahead.
        var offset = (now - state.EnteredAt).TotalMilliseconds - clientTime;
        if (state.ReferenceCount < ReferenceHeartbeats)
        {
            state.ReferenceOffset = state.ReferenceCount == 0 ? offset : Math.Min(state.ReferenceOffset, offset);
            state.ReferenceCount++;
            return null;
        }

        var lead = state.ReferenceOffset - offset;
        if (lead <= configuration.MaximumClockLeadMilliseconds)
        {
            return null;
        }

        ban = true;
        return $"a speedhack (the client clock is {lead / 1000:0.0} seconds ahead)";
    }

    private async ValueTask HandleViolationAsync(Player player, string violation, bool ban, GkzGuardConfiguration configuration)
    {
        if (this._states.TryGetValue(player, out var state))
        {
            state.LastViolation = violation;
        }

        player.Logger.LogWarning("Anti-cheat: player {player} (account {account}) was caught with {violation}.", player, player.Account?.LoginName, violation);
        var willBan = ban && configuration.BanOnDetection && configuration.DisconnectOnDetection;
        GkzSecurityIncidents.Report(player, "Anti-cheat", violation, willBan ? "banned" : configuration.DisconnectOnDetection ? "disconnected" : "logged");
        if (!configuration.DisconnectOnDetection)
        {
            return;
        }

        if (ban && configuration.BanOnDetection && player.Account is { } account)
        {
            player.Logger.LogError("Anti-cheat: banning account {account} for {violation}.", account.LoginName, violation);
            account.State = AccountState.Banned;
            await player.SaveProgressAsync().ConfigureAwait(false);
        }

        await player.ShowBlueMessageAsync("Anti-cheat: se detectó un programa o una modificación no permitida. Cerralo y volvé a entrar.").ConfigureAwait(false);
        await player.DisconnectAsync().ConfigureAwait(false);
    }

    private sealed class GuardState
    {
        public GuardState(uint challenge, DateTime enteredAt)
        {
            this.Challenge = challenge;
            this.EnteredAt = enteredAt;
            this.LastHeartbeat = enteredAt;
        }

        public uint Challenge { get; }

        public DateTime EnteredAt { get; }

        public DateTime LastHeartbeat { get; set; }

        public uint LastSequence { get; set; }

        public double ReferenceOffset { get; set; }

        public int ReferenceCount { get; set; }

        public string? LastViolation { get; set; }
    }
}
