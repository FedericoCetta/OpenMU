// <copyright file="GkzEventSchedule.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// When a periodic event runs, for the public website of MU GKZ.
/// </summary>
/// <param name="NextStartUtc">The next start by the timetable; <c>null</c> without a timetable.</param>
/// <param name="IsRunning">If the event is running right now.</param>
public record GkzEventSchedule(DateTime? NextStartUtc, bool IsRunning);

/// <summary>
/// A periodic event which can tell when it runs.
/// </summary>
public interface IGkzScheduledEvent
{
    /// <summary>
    /// Gets when the event runs on a game server.
    /// </summary>
    /// <param name="gameContext">The game context of the server.</param>
    /// <returns>The schedule; <c>null</c> when the event isn't configured.</returns>
    GkzEventSchedule? GetGkzSchedule(IGameContext gameContext);
}

/// <summary>
/// Calculates the next start of a timetable.
/// </summary>
public static class GkzTimetable
{
    /// <summary>
    /// Gets the next start of a timetable, which is in the time zone of the server.
    /// </summary>
    /// <param name="timetable">The start times of the day.</param>
    /// <param name="serverTimeZone">The time zone of the server.</param>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The next start in UTC; <c>null</c> for an empty timetable.</returns>
    public static DateTime? GetNextStartUtc(IEnumerable<TimeOnly> timetable, TimeZoneInfo serverTimeZone, DateTime utcNow)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(utcNow, serverTimeZone);
        DateTime? next = null;
        foreach (var time in timetable)
        {
            var candidate = nowLocal.Date + time.ToTimeSpan();
            if (candidate <= nowLocal)
            {
                candidate = candidate.AddDays(1);
            }

            if (next is null || candidate < next)
            {
                next = candidate;
            }
        }

        if (next is not { } nextLocal)
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), serverTimeZone);
        }
        catch (ArgumentException)
        {
            // The time doesn't exist in the time zone (a daylight saving gap); it starts an hour later.
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal.AddHours(1), DateTimeKind.Unspecified), serverTimeZone);
        }
    }
}
