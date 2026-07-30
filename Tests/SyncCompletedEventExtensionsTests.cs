#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using Xunit;
using NotionTaskSync.Events;

public class SyncCompletedEventExtensionsTests
{
    [Fact]
    public void ToOneLineSummary_ReturnsExpectedFormat()
    {
        // Arrange
        var evt = new SyncCompletedEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = new DateTime(2024, 7, 31, 12, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromMilliseconds(1234),
            Success = true,
            TasksProcessed = 10,
            ChangesDetected = 5,
            ConflictsResolved = 2,
            ErrorMessage = null
        };

        // Act
        var summary = evt.ToOneLineSummary();

        // Assert
        Assert.Contains("2024-07-31T12:00:00.0000000Z", summary);
        Assert.Contains("1234ms", summary);
        Assert.Contains("10 tasks", summary);
        Assert.Contains("5 changes", summary);
        Assert.Contains("2 conflicts", summary);
        Assert.Contains("Success", summary);
        Assert.DoesNotContain("Error", summary);
    }

    [Fact]
    public void ToDetailedReport_ReturnsAllProperties()
    {
        // Arrange
        var evt = new SyncCompletedEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = new DateTime(2024, 7, 31, 12, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromMilliseconds(1234),
            Success = false,
            TasksProcessed = 8,
            ChangesDetected = 3,
            ConflictsResolved = 1,
            ErrorMessage = "Network failure"
        };

        // Act
        var report = evt.ToDetailedReport();

        // Assert
        Assert.Contains($"Event ID: {evt.EventId}", report);
        Assert.Contains("Timestamp: 2024-07-31T12:00:00.0000000Z", report);
        Assert.Contains("Duration: 1234 ms", report);
        Assert.Contains("Status: Failed", report);
        Assert.Contains("Tasks Processed: 8", report);
        Assert.Contains("Changes Detected: 3", report);
        Assert.Contains("Conflicts Resolved: 1", report);
        Assert.Contains("Error Message: Network failure", report);
    }
}
