// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NotionTaskSync.Utils;

namespace NotionTaskSync.Tests;

public class TimeHelperTests
{
    [Fact]
    public void FormatTimeSpan_Seconds_ReturnsSecondsFormat()
    {
        TimeHelper.FormatTimeSpan(TimeSpan.FromSeconds(45)).Should().Be("45s");
    }

    [Fact]
    public void FormatTimeSpan_Minutes_ReturnsMinutesFormat()
    {
        TimeHelper.FormatTimeSpan(TimeSpan.FromMinutes(30)).Should().Be("30m");
    }

    [Fact]
    public void FormatTimeSpan_Hours_ReturnsHoursFormat()
    {
        TimeHelper.FormatTimeSpan(TimeSpan.FromHours(5)).Should().Be("5h");
    }

    [Fact]
    public void FormatTimeSpan_Days_ReturnsDaysFormat()
    {
        TimeHelper.FormatTimeSpan(TimeSpan.FromDays(3)).Should().Be("3d");
    }

    [Fact]
    public void FormatTimeSpan_ZeroSeconds_ReturnsZeroSeconds()
    {
        TimeHelper.FormatTimeSpan(TimeSpan.Zero).Should().Be("0s");
    }

    [Fact]
    public void DaysBetween_SameDay_ReturnsZero()
    {
        var today = DateTime.UtcNow;
        TimeHelper.DaysBetween(today, today).Should().Be(0);
    }

    [Fact]
    public void DaysBetween_OneDayApart_ReturnsOne()
    {
        var start = new DateTime(2026, 5, 20);
        var end = new DateTime(2026, 5, 21);
        TimeHelper.DaysBetween(start, end).Should().Be(1);
    }

    [Fact]
    public void DaysBetween_ReversedDates_ReturnsNegative()
    {
        var start = new DateTime(2026, 5, 21);
        var end = new DateTime(2026, 5, 20);
        TimeHelper.DaysBetween(start, end).Should().Be(-1);
    }

    [Fact]
    public void IsPast_YesterdayDate_ReturnsTrue()
    {
        TimeHelper.IsPast(DateTime.UtcNow.AddDays(-1)).Should().BeTrue();
    }

    [Fact]
    public void IsFuture_TomorrowDate_ReturnsTrue()
    {
        TimeHelper.IsFuture(DateTime.UtcNow.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_PastDueDate_ReturnsTrue()
    {
        TimeHelper.IsOverdue(DateTime.UtcNow.AddHours(-1)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_FutureDueDate_ReturnsFalse()
    {
        TimeHelper.IsOverdue(DateTime.UtcNow.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void ShouldSync_NullLastSyncTime_ReturnsTrue()
    {
        TimeHelper.ShouldSync(null, 60).Should().BeTrue();
    }

    [Fact]
    public void ShouldSync_RecentSync_ReturnsFalse()
    {
        TimeHelper.ShouldSync(DateTime.UtcNow, 3600).Should().BeFalse();
    }

    [Fact]
    public void ShouldSync_OldSync_ReturnsTrue()
    {
        TimeHelper.ShouldSync(DateTime.UtcNow.AddHours(-2), 3600).Should().BeTrue();
    }

    [Fact]
    public void CalculateNextSyncTime_NullLastSync_ReturnsNow()
    {
        var result = TimeHelper.CalculateNextSyncTime(null, 60);
        result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CalculateNextSyncTime_WithLastSync_AddsInterval()
    {
        var lastSync = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc);
        var result = TimeHelper.CalculateNextSyncTime(lastSync, 3600);
        result.Should().Be(lastSync.AddSeconds(3600));
    }

    [Fact]
    public void GetRelativeTime_JustNow_ReturnsJustNow()
    {
        TimeHelper.GetRelativeTime(DateTime.UtcNow).Should().Be("just now");
    }

    [Fact]
    public void GetRelativeTime_MinutesAgo_ReturnsMinutesFormat()
    {
        TimeHelper.GetRelativeTime(DateTime.UtcNow.AddMinutes(-5)).Should().Contain("m ago");
    }

    [Fact]
    public void GetRelativeTime_HoursAgo_ReturnsHoursFormat()
    {
        TimeHelper.GetRelativeTime(DateTime.UtcNow.AddHours(-3)).Should().Contain("h ago");
    }

    [Fact]
    public void GetRelativeTime_DaysAgo_ReturnsDaysFormat()
    {
        TimeHelper.GetRelativeTime(DateTime.UtcNow.AddDays(-3)).Should().Contain("d ago");
    }

    [Fact]
    public void GetRelativeTime_WeeksAgo_ReturnsFormattedDate()
    {
        TimeHelper.GetRelativeTime(DateTime.UtcNow.AddDays(-10)).Should().Contain("-");
    }

    [Fact]
    public void AreWithinInterval_CloseTimestamps_ReturnsTrue()
    {
        var t1 = DateTime.UtcNow;
        var t2 = t1.AddSeconds(5);
        TimeHelper.AreWithinInterval(t1, t2, TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void AreWithinInterval_FarTimestamps_ReturnsFalse()
    {
        var t1 = DateTime.UtcNow;
        var t2 = t1.AddMinutes(5);
        TimeHelper.AreWithinInterval(t1, t2, TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    [Fact]
    public void ParseDateTime_NullInput_ReturnsNull()
    {
        TimeHelper.ParseDateTime(null).Should().BeNull();
    }

    [Fact]
    public void ParseDateTime_EmptyInput_ReturnsNull()
    {
        TimeHelper.ParseDateTime("").Should().BeNull();
    }

    [Fact]
    public void ParseDateTime_InvalidFormat_ReturnsNull()
    {
        TimeHelper.ParseDateTime("not-a-date").Should().BeNull();
    }

    [Fact]
    public void GetTodayStart_ReturnsDateWithNoTime()
    {
        var result = TimeHelper.GetTodayStart();
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void GetTodayEnd_ReturnsEndOfDay()
    {
        var result = TimeHelper.GetTodayEnd();
        result.Hour.Should().Be(23);
        result.Minute.Should().Be(59);
        result.Second.Should().Be(59);
    }

    [Fact]
    public void GetWeekStart_ReturnsMonday()
    {
        var result = TimeHelper.GetWeekStart();
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void GetMonthStart_ReturnsFirstDayOfMonth()
    {
        var result = TimeHelper.GetMonthStart();
        result.Day.Should().Be(1);
    }

    [Fact]
    public void HoursSince_PastDate_ReturnsPositive()
    {
        TimeHelper.HoursSince(DateTime.UtcNow.AddHours(-2)).Should().BeApproximately(2.0, 0.1);
    }
}
