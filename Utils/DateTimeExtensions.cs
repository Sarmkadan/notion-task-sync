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
    /// <param name="dateTime">The timestamp to check.</param>
    /// <param name="days">Number of days to check within. Must be non-negative.</param>
    /// <returns>True if the timestamp is within the specified days from now; otherwise false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="days"/> is negative.</exception>
    public static bool IsWithinDays(this DateTime dateTime, int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be non-negative.");
        }

        var threshold = DateTime.UtcNow.AddDays(-days);
        return dateTime >= threshold;
    }

    /// <summary>
    /// Determines if a timestamp is more recent than another timestamp.
    /// Used extensively in change detection and conflict resolution.
    /// </summary>
    /// <param name="dateTime">The timestamp to compare.</param>
    /// <param name="comparison">The timestamp to compare against.</param>
    /// <returns>True if the timestamp is newer than the comparison timestamp; otherwise false.</returns>
    public static bool IsNewerThan(this DateTime dateTime, DateTime comparison)
    {
        return dateTime > comparison;
    }

    /// <summary>
    /// Rounds a DateTime to the nearest minute boundary.
    /// Eliminates sub-minute precision differences that could cause false positives in change detection.
    /// </summary>
    /// <param name="dateTime">The timestamp to round.</param>
    /// <returns>The rounded DateTime at the nearest minute boundary.</returns>
    public static DateTime RoundToMinute(this DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMinute));
    }

    /// <summary>
    /// Rounds a DateTime to the nearest second boundary.
    /// Ensures consistent timestamp granularity across different systems.
    /// </summary>
    /// <param name="dateTime">The timestamp to round.</param>
    /// <returns>The rounded DateTime at the nearest second boundary.</returns>
    public static DateTime RoundToSecond(this DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
    }

    /// <summary>
    /// Converts a DateTime to ISO 8601 format string.
    /// Standard format used in API communications and logging.
    /// </summary>
    /// <param name="dateTime">The timestamp to format.</param>
    /// <returns>ISO 8601 formatted string.</returns>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("O");
    }

    /// <summary>
    /// Converts a DateTime to a human-readable format suitable for logging and display.
    /// </summary>
    /// <param name="dateTime">The timestamp to format.</param>
    /// <returns>Human-readable formatted string.</returns>
    public static string ToUserFriendlyString(this DateTime dateTime)
    {
        return dateTime.ToString("g");
    }

    /// <summary>
    /// Calculates the time elapsed since the given timestamp in a human-readable format.
    /// Returns strings like "5 minutes ago", "2 hours ago", "3 days ago".
    /// </summary>
    /// <param name="dateTime">The timestamp to calculate from.</param>
    /// <returns>Human-readable time ago string.</returns>
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
    /// <param name="dateTime">The first timestamp.</param>
    /// <param name="other">The second timestamp to compare with.</param>
    /// <returns>True if both timestamps represent the same day; otherwise false.</returns>
    public static bool IsSameDay(this DateTime dateTime, DateTime other)
    {
        return dateTime.Date == other.Date;
    }

    /// <summary>
    /// Converts a DateTime to a Unix timestamp (seconds since epoch).
    /// Used for integration with systems that prefer Unix timestamps.
    /// </summary>
    /// <param name="dateTime">The timestamp to convert.</param>
    /// <returns>Unix timestamp in seconds.</returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a Unix timestamp to a DateTime object.
    /// Complements ToUnixTimestamp for round-trip conversions.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp in seconds.</param>
    /// <returns>Converted DateTime in UTC.</returns>
    public static DateTime FromUnixTimestamp(this long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
    }

    /// <summary>
    /// Determines if a timestamp is stale based on a maximum age in hours.
    /// Used to identify data that needs refresh from the source system.
    /// </summary>
    /// <param name="dateTime">The timestamp to check.</param>
    /// <param name="maxAgeHours">Maximum age in hours before data is considered stale. Must be non-negative.</param>
    /// <returns>True if the timestamp is older than the maximum age; otherwise false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAgeHours"/> is negative.</exception>
    public static bool IsStale(this DateTime dateTime, int maxAgeHours)
    {
        if (maxAgeHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAgeHours), "Max age hours must be non-negative.");
        }

        var maxAge = DateTime.UtcNow.AddHours(-maxAgeHours);
        return dateTime < maxAge;
    }

    /// <summary>
    /// Gets the start of the day (00:00:00) for a given DateTime.
    /// Useful for range queries and date-based operations.
    /// </summary>
    /// <param name="dateTime">The timestamp to get start of day for.</param>
    /// <returns>DateTime at start of day (00:00:00).</returns>
    public static DateTime GetStartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999...) for a given DateTime.
    /// Complements GetStartOfDay for complete day range operations.
    /// </summary>
    /// <param name="dateTime">The timestamp to get end of day for.</param>
    /// <returns>DateTime at end of day (23:59:59.999...).</returns>
    public static DateTime GetEndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }
}