#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using NotionTaskSync.Constants;
using System;

/// <summary>
/// Provides time and date utilities for the application.
/// Handles formatting, parsing, and time-related calculations.
/// </summary>
public static class TimeHelper
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    public static DateTime GetCurrentUtcTime()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Formats a DateTime as a string using the application's standard format.
    /// </summary>
    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString(AppConstants.LastSyncTimeFormat);
    }

    /// <summary>
    /// Parses a string into a DateTime using the application's standard format.
    /// </summary>
    public static DateTime? ParseDateTime(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParseExact(
            dateString,
            AppConstants.LastSyncTimeFormat,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Determines if a given date is in the past.
    /// </summary>
    public static bool IsPast(DateTime dateTime)
    {
        return dateTime < GetCurrentUtcTime();
    }

    /// <summary>
    /// Determines if a given date is in the future.
    /// </summary>
    public static bool IsFuture(DateTime dateTime)
    {
        return dateTime > GetCurrentUtcTime();
    }

    /// <summary>
    /// Calculates the number of days between two dates.
    /// </summary>
    public static int DaysBetween(DateTime from, DateTime to)
    {
        return (int)(to.Date - from.Date).TotalDays;
    }

    /// <summary>
    /// Calculates the number of hours since a given time.
    /// </summary>
    public static double HoursSince(DateTime dateTime)
    {
        return (GetCurrentUtcTime() - dateTime).TotalHours;
    }

    /// <summary>
    /// Determines if a date is overdue based on current time.
    /// </summary>
    public static bool IsOverdue(DateTime dueDate)
    {
        return dueDate < GetCurrentUtcTime();
    }

    /// <summary>
    /// Formats a TimeSpan into a human-readable string.
    /// </summary>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return $"{(int)timeSpan.TotalSeconds}s";

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m";

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h";

        return $"{(int)timeSpan.TotalDays}d";
    }

    /// <summary>
    /// Gets a human-readable string for how long ago a date was.
    /// </summary>
    public static string GetRelativeTime(DateTime dateTime)
    {
        var elapsed = GetCurrentUtcTime() - dateTime;

        if (elapsed.TotalSeconds < 60)
            return "just now";

        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes}m ago";

        if (elapsed.TotalHours < 24)
            return $"{(int)elapsed.TotalHours}h ago";

        if (elapsed.TotalDays < 7)
            return $"{(int)elapsed.TotalDays}d ago";

        return dateTime.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Determines if a sync should run based on the last sync time and interval.
    /// </summary>
    public static bool ShouldSync(DateTime? lastSyncTime, int intervalSeconds)
    {
        if (lastSyncTime is null)
            return true;

        var elapsed = GetCurrentUtcTime() - lastSyncTime.Value;
        return elapsed.TotalSeconds >= intervalSeconds;
    }

    /// <summary>
    /// Calculates when the next sync should occur.
    /// </summary>
    public static DateTime CalculateNextSyncTime(DateTime? lastSyncTime, int intervalSeconds)
    {
        if (lastSyncTime is null)
            return GetCurrentUtcTime();

        return lastSyncTime.Value.AddSeconds(intervalSeconds);
    }

    /// <summary>
    /// Gets the start of the current day in UTC.
    /// </summary>
    public static DateTime GetTodayStart()
    {
        return GetCurrentUtcTime().Date;
    }

    /// <summary>
    /// Gets the end of the current day in UTC.
    /// </summary>
    public static DateTime GetTodayEnd()
    {
        return GetCurrentUtcTime().Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets the start of the current week in UTC.
    /// </summary>
    public static DateTime GetWeekStart()
    {
        var today = GetCurrentUtcTime();
        var diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        return today.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gets the start of the current month in UTC.
    /// </summary>
    public static DateTime GetMonthStart()
    {
        var today = GetCurrentUtcTime();
        return new DateTime(today.Year, today.Month, 1);
    }

    /// <summary>
    /// Determines if two times are within a specified interval of each other.
    /// </summary>
    public static bool AreWithinInterval(DateTime time1, DateTime time2, TimeSpan interval)
    {
        var diff = Math.Abs((time2 - time1).TotalSeconds);
        return diff <= interval.TotalSeconds;
    }
}
