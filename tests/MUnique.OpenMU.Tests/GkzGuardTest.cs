// <copyright file="GkzGuardTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.GameLogic.Views.AntiCheat;

/// <summary>
/// Tests for the anti-cheat of the MU GKZ client on the server (<see cref="GkzGuardPlugIn"/>).
/// </summary>
[TestFixture]
public class GkzGuardTest
{
    private DateTime _now;
    private GkzGuardPlugIn _guard = null!;
    private Player _player = null!;
    private uint _challenge;
    private uint _sequence;

    /// <summary>
    /// Sets up a player who entered the game and got the challenge.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._now = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        GkzSecurityIncidents.FilePath = null;
        GkzSecurityIncidents.ClearForTests();
        this._sequence = 0;
        this._guard = new GkzGuardPlugIn(() => this._now) { Configuration = new GkzGuardConfiguration() };
        this._player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        Mock.Get(this._player.ViewPlugIns.GetPlugIn<IShowGkzGuardChallengePlugIn>()!)
            .Setup(v => v.ShowChallengeAsync(It.IsAny<uint>()))
            .Callback<uint>(c => this._challenge = c)
            .Returns(ValueTask.CompletedTask);
        await this._guard.PlayerStateChangedAsync(this._player, PlayerState.CharacterSelection, PlayerState.EnteredWorld).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that a client which sends correct heartbeats in time stays in the game.
    /// </summary>
    [Test]
    public async Task CorrectHeartbeatsAreFineAsync()
    {
        for (int i = 0; i < 30; i++)
        {
            this._now += TimeSpan.FromSeconds(10);
            await this.SendHeartbeatAsync(clientTime: (uint)(i * 10_000)).ConfigureAwait(false);
            await this._guard.ExecuteTaskAsync((GameContext)this._player.GameContext).ConfigureAwait(false);
        }

        Assert.That(this.IsDisconnected, Is.False);
    }

    /// <summary>
    /// Tests that a heartbeat with a wrong proof (a bot or a packet editor) disconnects the player.
    /// </summary>
    [Test]
    public async Task ForgedHeartbeatDisconnectsAsync()
    {
        this._now += TimeSpan.FromSeconds(10);
        await this._guard.HandleHeartbeatAsync(this._player, 1, 10_000, 0, 0, 12345).ConfigureAwait(false);

        Assert.That(this.IsDisconnected, Is.True);
        Assert.That(GkzSecurityIncidents.GetRecent(), Has.Some.Matches<SecurityIncident>(i => i.Kind == "Anti-cheat" && i.Account == this._player.Account!.LoginName));
    }

    /// <summary>
    /// Tests that a replayed heartbeat disconnects the player.
    /// </summary>
    [Test]
    public async Task ReplayedHeartbeatDisconnectsAsync()
    {
        this._now += TimeSpan.FromSeconds(10);
        await this.SendHeartbeatAsync(clientTime: 10_000).ConfigureAwait(false);
        this._sequence--;
        await this.SendHeartbeatAsync(clientTime: 10_000).ConfigureAwait(false);

        Assert.That(this.IsDisconnected, Is.True);
    }

    /// <summary>
    /// Tests that a speedhack (the client clock runs 1.5 times faster) is detected within two minutes.
    /// </summary>
    [Test]
    public async Task SpeedhackIsDetectedByTheClockAsync()
    {
        for (int i = 1; i <= 12 && !this.IsDisconnected; i++)
        {
            this._now += TimeSpan.FromSeconds(10);
            await this.SendHeartbeatAsync(clientTime: (uint)(i * 15_000)).ConfigureAwait(false);
        }

        Assert.That(this.IsDisconnected, Is.True);
    }

    /// <summary>
    /// Tests that heartbeats which are delayed by the network and arrive in a burst aren't a speedhack.
    /// </summary>
    [Test]
    public async Task NetworkDelaysAreNoSpeedhackAsync()
    {
        var start = this._now;
        for (int i = 1; i <= 40; i++)
        {
            // The client sends every 10 seconds; every fifth heartbeat is delayed by 8 seconds, every seventh by 3.
            var clientTime = (uint)(i * 10_000);
            var delay = i % 5 == 0 ? TimeSpan.FromSeconds(8) : i % 7 == 0 ? TimeSpan.FromSeconds(3) : TimeSpan.Zero;
            this._now = start + TimeSpan.FromMilliseconds(clientTime) + delay;
            await this.SendHeartbeatAsync(clientTime).ConfigureAwait(false);
        }

        Assert.That(this.IsDisconnected, Is.False);
    }

    /// <summary>
    /// Tests that slow first heartbeats (the server is busy when the player enters) don't count as a speedhack later.
    /// </summary>
    [Test]
    public async Task SlowFirstHeartbeatsAreNoSpeedhackAsync()
    {
        var start = this._now;
        for (int i = 1; i <= 30; i++)
        {
            var clientTime = (uint)(i * 10_000);
            var delay = i <= 4 ? TimeSpan.FromSeconds(6) : TimeSpan.Zero;
            this._now = start + TimeSpan.FromMilliseconds(clientTime) + delay;
            await this.SendHeartbeatAsync(clientTime).ConfigureAwait(false);
        }

        Assert.That(this.IsDisconnected, Is.False);
    }

    /// <summary>
    /// Tests that a client without heartbeats (a bot or the original client) is disconnected.
    /// </summary>
    [Test]
    public async Task MissingHeartbeatsDisconnectAsync()
    {
        var gameContext = (GameContext)this._player.GameContext;
        await gameContext.AddPlayerAsync(this._player).ConfigureAwait(false);

        this._now += TimeSpan.FromSeconds(60);
        await this._guard.ExecuteTaskAsync(gameContext).ConfigureAwait(false);
        Assert.That(this.IsDisconnected, Is.False, "too early");

        this._now += TimeSpan.FromSeconds(40);
        await this._guard.ExecuteTaskAsync(gameContext).ConfigureAwait(false);
        Assert.That(this.IsDisconnected, Is.True);
    }

    /// <summary>
    /// Tests that a reported Cheat Engine disconnects the player.
    /// </summary>
    [Test]
    public async Task ForbiddenProgramDisconnectsAsync()
    {
        this._now += TimeSpan.FromSeconds(10);
        await this.SendHeartbeatAsync(clientTime: 10_000, GkzGuardFlags.ForbiddenProgram, detail: 0).ConfigureAwait(false);

        Assert.That(this.IsDisconnected, Is.True);
    }

    private bool IsDisconnected => this._guard.GetLastViolation(this._player) is not null;

    private ValueTask SendHeartbeatAsync(uint clientTime, GkzGuardFlags flags = GkzGuardFlags.None, uint detail = 0)
    {
        var sequence = ++this._sequence;
        var proof = GkzGuardProof.Compute(GkzGuardProof.Secret, this._challenge, sequence, clientTime, (uint)flags, detail);
        return this._guard.HandleHeartbeatAsync(this._player, sequence, clientTime, (uint)flags, detail, proof);
    }
}
