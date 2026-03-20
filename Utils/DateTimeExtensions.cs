#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;

/// <summary>
/// Extension methods for DateTime operations commonly used in sync scenarios.
/// Provides utilities for timestamp comparisons, timezone handling, and formatting.
/// Simplifies recurring patterns throughout the codebase by centralizing logic.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Determines if a timestamp falls within a specified number of days from now.
    /// Useful for detecting "recent" changes in sync operations.
    /// </summary>
    public static bool IsWithinDays(this DateTime dateTime, int days)
    {
        var threshold = DateTime.UtcNow.AddDays(-Math.Abs(days));
        return dateTime >= threshold;
    }

    /// <summary>
    /// Determines if a timestamp is more recent than another timestamp.
    /// Used extensively in change detection and conflict resolution.
    /// </summary>
    public static bool IsNewerThan(this DateTime dateTime, DateTime comparison)
    {
        return dateTime > comparison;
    }

    /// <summary>
    /// Rounds a DateTime to the nearest minute boundary.
    /// Eliminates sub-minute precision differences that could cause false positives in change detection.
    /// </summary>
    public static DateTime RoundToMinute(this DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMinute));
    }

    /// <summary>
    /// Rounds a DateTime to the nearest second boundary.
    /// Ensures consistent timestamp granularity across different systems.
    /// </summary>
    public static DateTime RoundToSecond(this DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
    }

    /// <summary>
    /// Converts a DateTime to ISO 8601 format string.
    /// Standard format used in API communications and logging.
    /// </summary>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("O");
    }

    /// <summary>
    /// Converts a DateTime to a human-readable format suitable for logging and display.
    /// </summary>
    public static string ToUserFriendlyString(this DateTime dateTime)
    {
        return dateTime.ToString("g");
    }

    /// <summary>
    /// Calculates the time elapsed since the given timestamp in a human-readable format.
    /// Returns strings like "5 minutes ago", "2 hours ago", "3 days ago".
    /// </summary>
    public static string ToTimeAgoString(this DateTime dateTime)
    {
        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalSeconds < 60
            ? "just now"
            : elapsed.TotalMinutes < 60
                ? $"{(int)elapsed.TotalMinutes} minutes ago"
                : elapsed.TotalHours < 24
                    ? $"{(int)elapsed.TotalHours} hours ago"
                    : elapsed.TotalDays < 30
                        ? $"{(int)elapsed.TotalDays} days ago"
                        : $"{(int)(elapsed.TotalDays / 30)} months ago";
    }

    /// <summary>
    /// Determines if two DateTime objects represent the same day in UTC.
    /// Ignores time-of-day differences, useful for date-based comparisons.
    /// </summary>
    public static bool IsSameDay(this DateTime dateTime, DateTime other)
    {
        return dateTime.Date == other.Date;
    }

    /// <summary>
    /// Converts a DateTime to a Unix timestamp (seconds since epoch).
    /// Used for integration with systems that prefer Unix timestamps.
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a Unix timestamp to a DateTime object.
    /// Complements ToUnixTimestamp for round-trip conversions.
    /// </summary>
    public static DateTime FromUnixTimestamp(this long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
    }

    /// <summary>
    /// Determines if a timestamp is stale based on a maximum age in hours.
    /// Used to identify data that needs refresh from the source system.
    /// </summary>
    public static bool IsStale(this DateTime dateTime, int maxAgeHours)
    {
        var maxAge = DateTime.UtcNow.AddHours(-maxAgeHours);
        return dateTime < maxAge;
    }

    /// <summary>
    /// Gets the start of the day (00:00:00) for a given DateTime.
    /// Useful for range queries and date-based operations.
    /// </summary>
    public static DateTime GetStartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999...) for a given DateTime.
    /// Complements GetStartOfDay for complete day range operations.
    /// </summary>
    public static DateTime GetEndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }
}
