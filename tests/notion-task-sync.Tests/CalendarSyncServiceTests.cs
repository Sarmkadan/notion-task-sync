#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CalendarSyncService"/>.
/// </summary>
public class CalendarSyncServiceTests
{
    private static Mock<ITaskRepository> BuildRepoWithTasks(params Domain.Models.Task[] tasks)
    {
        var mock = new Mock<ITaskRepository>();
        mock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(tasks.ToList());
        mock.Setup(r => r.AddAsync(It.IsAny<Domain.Models.Task>()))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.UpdateAsync(It.IsAny<Domain.Models.Task>()))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.SaveAsync())
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task ExportToCalendar_WritesIcsFileWithCorrectEventCount()
    {
        // Arrange
        var tasks = new[]
        {
            new Domain.Models.Task { Title = "Task A", DueDate = new DateTime(2026, 6, 1) },
            new Domain.Models.Task { Title = "Task B", DueDate = new DateTime(2026, 6, 15) },
            new Domain.Models.Task { Title = "No DueDate" }    // should be excluded
        };
        var repo = BuildRepoWithTasks(tasks);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CalendarSyncService>.Instance;
        var service = new CalendarSyncService(repo.Object, logger);

        var tmpFile = Path.GetTempFileName();
        try
        {
            // Act
            var result = await service.ExportToCalendarAsync(tmpFile);

            // Assert
            result.EventsExported.Should().Be(2); // only tasks with due dates
            var content = await File.ReadAllTextAsync(tmpFile);
            content.Should().Contain("BEGIN:VCALENDAR");
            content.Should().Contain("SUMMARY:Task A");
            content.Should().Contain("SUMMARY:Task B");
            content.Should().NotContain("No DueDate");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task ImportFromCalendar_CreatesNewTasksForUnknownEvents()
    {
        // Arrange — repository starts empty
        var addedTasks = new List<Domain.Models.Task>();
        var mock = new Mock<ITaskRepository>();
        mock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Domain.Models.Task>());
        mock.Setup(r => r.AddAsync(It.IsAny<Domain.Models.Task>()))
            .Callback<Domain.Models.Task>(t => addedTasks.Add(t))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CalendarSyncService>.Instance;
        var service = new CalendarSyncService(mock.Object, logger);

        var icsContent = """
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//EN
BEGIN:VEVENT
UID:test-event-001@example.com
SUMMARY:Imported Task
DTSTART;VALUE=DATE:20260701
DTEND;VALUE=DATE:20260702
END:VEVENT
END:VCALENDAR
""";
        var tmpFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmpFile, icsContent);

            // Act
            var result = await service.ImportFromCalendarAsync(tmpFile);

            // Assert
            result.EventsImported.Should().Be(1);
            result.TasksCreated.Should().Be(1);
            addedTasks.Should().HaveCount(1);
            addedTasks[0].Title.Should().Be("Imported Task");
            addedTasks[0].DueDate.Should().Be(new DateTime(2026, 7, 1));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task ImportFromCalendar_UpdatesExistingTaskDueDate_WhenUidMatches()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = new Domain.Models.Task
        {
            Id = taskId,
            Title = "My Task",
            DueDate = new DateTime(2026, 5, 1)
        };

        var updatedTasks = new List<Domain.Models.Task>();
        var mock = new Mock<ITaskRepository>();
        mock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Domain.Models.Task> { existingTask });
        mock.Setup(r => r.UpdateAsync(It.IsAny<Domain.Models.Task>()))
            .Callback<Domain.Models.Task>(t => updatedTasks.Add(t))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CalendarSyncService>.Instance;
        var service = new CalendarSyncService(mock.Object, logger);

        var icsContent = $"""
BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
UID:task-{taskId}@notion-task-sync
SUMMARY:My Task
DTSTART;VALUE=DATE:20260801
DTEND;VALUE=DATE:20260802
END:VEVENT
END:VCALENDAR
""";
        var tmpFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmpFile, icsContent);

            // Act
            var result = await service.ImportFromCalendarAsync(tmpFile);

            // Assert
            result.TasksUpdated.Should().Be(1);
            result.TasksCreated.Should().Be(0);
            updatedTasks.Should().HaveCount(1);
            updatedTasks[0].DueDate.Should().Be(new DateTime(2026, 8, 1));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task ExportToCalendar_ExcludesDeletedTasks()
    {
        var tasks = new[]
        {
            new Domain.Models.Task { Title = "Alive", DueDate = new DateTime(2026, 6, 1), IsDeleted = false },
            new Domain.Models.Task { Title = "Deleted", DueDate = new DateTime(2026, 6, 2), IsDeleted = true }
        };
        var repo = BuildRepoWithTasks(tasks);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CalendarSyncService>.Instance;
        var service = new CalendarSyncService(repo.Object, logger);

        var tmpFile = Path.GetTempFileName();
        try
        {
            var result = await service.ExportToCalendarAsync(tmpFile);

            result.EventsExported.Should().Be(1);
            var content = await File.ReadAllTextAsync(tmpFile);
            content.Should().Contain("SUMMARY:Alive");
            content.Should().NotContain("SUMMARY:Deleted");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
