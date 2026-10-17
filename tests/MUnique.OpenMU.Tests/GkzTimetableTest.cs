// <copyright file="GkzTimetableTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Tests for the next start of the events on the public website of MU GKZ.
/// </summary>
[TestFixture]
public class GkzTimetableTest
{
    private static readonly TimeZoneInfo MinusThree = TimeZoneInfo.CreateCustomTimeZone("GKZ-3", TimeSpan.FromHours(-3), "GKZ-3", "GKZ-3");

    /// <summary>
    /// Tests that the next start is the first time of the timetable after now, in the time zone of the server.
    /// </summary>
    [Test]
    public void NextStartIsTheNextTimeOfTheDay()
    {
        var utcNow = new DateTime(2026, 10, 17, 15, 30, 0, DateTimeKind.Utc); // 12:30 en el server
        var next = GkzTimetable.GetNextStartUtc([new TimeOnly(10, 0), new TimeOnly(14, 0), new TimeOnly(12, 0)], MinusThree, utcNow);
        Assert.That(next, Is.EqualTo(new DateTime(2026, 10, 17, 17, 0, 0, DateTimeKind.Utc)));
    }

    /// <summary>
    /// Tests that after the last time of the day, the next start is the first one of the next day.
    /// </summary>
    [Test]
    public void AfterTheLastTimeItsTomorrow()
    {
        var utcNow = new DateTime(2026, 10, 18, 2, 0, 0, DateTimeKind.Utc); // 23:00 del 17 en el server
        var next = GkzTimetable.GetNextStartUtc([new TimeOnly(10, 0), new TimeOnly(20, 0)], MinusThree, utcNow);
        Assert.That(next, Is.EqualTo(new DateTime(2026, 10, 18, 13, 0, 0, DateTimeKind.Utc)));
    }

    /// <summary>
    /// Tests that an empty timetable has no next start.
    /// </summary>
    [Test]
    public void EmptyTimetableHasNoStart()
    {
        Assert.That(GkzTimetable.GetNextStartUtc([], TimeZoneInfo.Utc, DateTime.UtcNow), Is.Null);
    }
}
