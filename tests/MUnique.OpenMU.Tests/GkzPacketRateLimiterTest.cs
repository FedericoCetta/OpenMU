// <copyright file="GkzPacketRateLimiterTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameServer.MessageHandler.Gkz;

/// <summary>
/// Tests for the rate limit of the MU GKZ client packets.
/// </summary>
[TestFixture]
public class GkzPacketRateLimiterTest
{
    private const byte MarketPage = 0x61;
    private const byte SplitStack = 0x40;

    private DateTime _now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tests that a flood of expensive requests is cut after the burst, and allowed again after a while.
    /// </summary>
    [Test]
    public async Task ExpensiveRequestsAreLimitedAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var limiter = new GkzPacketRateLimiter(() => this._now);

        var allowed = Enumerable.Range(0, 100).Count(_ => limiter.TryAcquire(player, MarketPage));
        Assert.That(allowed, Is.EqualTo((int)GkzPacketRateLimiter.Limits.ExpensiveCapacity));

        this._now += TimeSpan.FromSeconds(1);
        Assert.That(limiter.TryAcquire(player, MarketPage), Is.True);
    }

    /// <summary>
    /// Tests that the cheap requests have their own, higher limit, which a normal client never reaches.
    /// </summary>
    [Test]
    public async Task CheapRequestsHaveAHigherLimitAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var limiter = new GkzPacketRateLimiter(() => this._now);

        var allowed = Enumerable.Range(0, 100).Count(_ => limiter.TryAcquire(player, SplitStack));
        Assert.That(allowed, Is.EqualTo((int)GkzPacketRateLimiter.Limits.Capacity));

        // Ten clicks per second for a minute are fine.
        for (int i = 0; i < 600; i++)
        {
            this._now += TimeSpan.FromMilliseconds(100);
            Assert.That(limiter.TryAcquire(player, SplitStack), Is.True);
        }
    }
}
