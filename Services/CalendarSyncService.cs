#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

/// <summary>
/// Provides two-way synchronization between local tasks and iCal (.ics) calendar files.
/// Exports tasks with due dates as VEVENT entries and imports calendar events back as tasks,
/// making due dates visible in any calendar application that supports the iCal standard.
/// </summary>
public class CalendarSyncService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<CalendarSyncService> _logger;

    private const string ICalVersion = "2.0";
    private const string ProductId = "-//NotionTaskSync//EN";
    private const string DefaultCalendarFile = "notion-tasks.ics";

    /// <summary>
    /// Initialises a new instance of <see cref="CalendarSyncService"/>.
    /// </summary>
    public CalendarSyncService(
        ITaskRepository taskRepository,
        ILogger<CalendarSyncService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all tasks that have a due date to an iCal file.
    /// Each task becomes a VEVENT with the due date as DTSTART/DTEND.
    /// </summary>
    /// <param name="outputPath">Path to write the .ics file. Defaults to <c>notion-tasks.ics</c> in the current directory.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>A <see cref="CalendarSyncResult"/> with export statistics.</returns>
    public async Task<CalendarSyncResult> ExportToCalendarAsync(
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CalendarSyncResult();
        var filePath = outputPath ?? DefaultCalendarFile;

        _logger.LogInformation("Exporting tasks to calendar file: {Path}", filePath);

        var tasks = await _taskRepository.GetAllAsync();
        var tasksWithDueDates = tasks
            .Where(t => t.DueDate.HasValue && !t.IsDeleted)
            .ToList();

        _logger.LogInformation("Found {Count} tasks with due dates to export", tasksWithDueDates.Count);

        var events = tasksWithDueDates
            .Select(TaskToCalendarEvent)
            .ToList();

        var icsContent = BuildICalContent(events);
        await File.WriteAllTextAsync(filePath, icsContent, Encoding.UTF8, cancellationToken);

        result.EventsExported = events.Count;
        _logger.LogInformation("Exported {Count} events to {Path}", result.EventsExported, filePath);

        return result;
    }

    /// <summary>
    /// Imports calendar events from an iCal file and creates or updates tasks accordingly.
    /// Events without a matching task (by UID) create new tasks; events linked to an existing
    /// task update its due date.
    /// </summary>
    /// <param name="inputPath">Path to the .ics file to import.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>A <see cref="CalendarSyncResult"/> with import statistics.</returns>
    public async Task<CalendarSyncResult> ImportFromCalendarAsync(
        string inputPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Calendar file not found: {inputPath}", inputPath);

        var result = new CalendarSyncResult();

        _logger.LogInformation("Importing calendar events from: {Path}", inputPath);

        var icsContent = await File.ReadAllTextAsync(inputPath, cancellationToken);
        var events = ParseICalContent(icsContent);

        _logger.LogInformation("Parsed {Count} events from calendar file", events.Count);

        var existingTasks = await _taskRepository.GetAllAsync();

        foreach (var calEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchingTask = FindMatchingTask(existingTasks, calEvent);

            if (matchingTask is not null)
            {
                if (matchingTask.DueDate != calEvent.StartDate.Date)
                {
                    matchingTask.DueDate = calEvent.StartDate.Date;
                    matchingTask.UpdateTimestamp();
                    await _taskRepository.UpdateAsync(matchingTask);
                    result.TasksUpdated++;
                    _logger.LogDebug("Updated due date for task '{Title}'", matchingTask.Title);
                }
                else
                {
                    result.Skipped++;
                }
            }
            else
            {
                var newTask = CalendarEventToTask(calEvent);
                await _taskRepository.AddAsync(newTask);
                result.TasksCreated++;
                _logger.LogDebug("Created new task from calendar event '{Title}'", calEvent.Title);
            }

            result.EventsImported++;
        }

        await _taskRepository.SaveAsync();

        _logger.LogInformation(
            "Import complete: {Created} created, {Updated} updated, {Skipped} skipped",
            result.TasksCreated, result.TasksUpdated, result.Skipped);

        return result;
    }

    /// <summary>
    /// Performs a full bidirectional sync: exports current tasks then re-imports the
    /// resulting .ics file so any manual edits in external calendar apps are picked up.
    /// </summary>
    /// <param name="calendarFilePath">Path to the shared .ics file used as the sync endpoint.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>A combined <see cref="CalendarSyncResult"/> for the entire operation.</returns>
    public async Task<CalendarSyncResult> BidirectionalSyncAsync(
        string calendarFilePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bidirectional calendar sync with: {Path}", calendarFilePath);

        var exportResult = await ExportToCalendarAsync(calendarFilePath, cancellationToken);
        var importResult = await ImportFromCalendarAsync(calendarFilePath, cancellationToken);

        return new CalendarSyncResult
        {
            EventsExported = exportResult.EventsExported,
            EventsImported = importResult.EventsImported,
            TasksCreated = importResult.TasksCreated,
            TasksUpdated = importResult.TasksUpdated,
            Skipped = importResult.Skipped,
            Warnings = exportResult.Warnings.Concat(importResult.Warnings).ToList()
        };
    }

    // -------------------------------------------------------------------------
    // iCal rendering helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a valid iCal VCALENDAR string from the provided event list.
    /// </summary>
    private static string BuildICalContent(IEnumerable<CalendarEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine($"VERSION:{ICalVersion}");
        sb.AppendLine($"PRODID:{ProductId}");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");

        foreach (var ev in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{ev.ExternalUid ?? ev.Id.ToString()}");
            sb.AppendLine($"SUMMARY:{EscapeICalText(ev.Title)}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{ev.StartDate:yyyyMMdd}");
            sb.AppendLine($"DTEND;VALUE=DATE:{(ev.EndDate ?? ev.StartDate.AddDays(1)):yyyyMMdd}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"CREATED:{ev.CreatedAt:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"LAST-MODIFIED:{ev.UpdatedAt:yyyyMMddTHHmmssZ}");

            if (!string.IsNullOrWhiteSpace(ev.Description))
                sb.AppendLine($"DESCRIPTION:{EscapeICalText(ev.Description)}");

            if (!string.IsNullOrWhiteSpace(ev.Location))
                sb.AppendLine($"LOCATION:{EscapeICalText(ev.Location)}");

            if (ev.LinkedTaskId.HasValue)
                sb.AppendLine($"X-NOTION-TASK-ID:{ev.LinkedTaskId}");

            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    /// <summary>
    /// Parses iCal text into a list of <see cref="CalendarEvent"/> objects.
    /// Handles basic VEVENT blocks; only SUMMARY, DTSTART, DTEND, UID,
    /// DESCRIPTION, LOCATION, and X-NOTION-TASK-ID properties are extracted.
    /// </summary>
    private static List<CalendarEvent> ParseICalContent(string icsContent)
    {
        var events = new List<CalendarEvent>();
        var lines = icsContent.Replace("\r\n", "\n").Split('\n');

        CalendarEvent? current = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line == "BEGIN:VEVENT")
            {
                current = new CalendarEvent
                {
                    Title = string.Empty,
                    Source = CalendarEventSource.Import
                };
                continue;
            }

            if (line == "END:VEVENT" && current is not null)
            {
                if (!string.IsNullOrWhiteSpace(current.Title))
                    events.Add(current);
                current = null;
                continue;
            }

            if (current is null)
                continue;

            if (line.StartsWith("SUMMARY:"))
                current.Title = UnescapeICalText(line["SUMMARY:".Length..]);
            else if (line.StartsWith("DTSTART"))
                current.StartDate = ParseICalDate(line);
            else if (line.StartsWith("DTEND"))
                current.EndDate = ParseICalDate(line);
            else if (line.StartsWith("UID:"))
                current.ExternalUid = line["UID:".Length..];
            else if (line.StartsWith("DESCRIPTION:"))
                current.Description = UnescapeICalText(line["DESCRIPTION:".Length..]);
            else if (line.StartsWith("LOCATION:"))
                current.Location = UnescapeICalText(line["LOCATION:".Length..]);
            else if (line.StartsWith("X-NOTION-TASK-ID:") && Guid.TryParse(line["X-NOTION-TASK-ID:".Length..], out var taskId))
                current.LinkedTaskId = taskId;
        }

        return events;
    }

    // -------------------------------------------------------------------------
    // Conversion helpers
    // -------------------------------------------------------------------------

    private static CalendarEvent TaskToCalendarEvent(Domain.Models.Task task) =>
        new()
        {
            Title = task.Title,
            Description = task.Description,
            StartDate = task.DueDate!.Value.Date,
            EndDate = task.DueDate.Value.Date.AddDays(1),
            IsAllDay = true,
            ExternalUid = $"task-{task.Id}@notion-task-sync",
            LinkedTaskId = task.Id,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Source = CalendarEventSource.Task
        };

    private static Domain.Models.Task CalendarEventToTask(CalendarEvent ev) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = ev.Title,
            Description = ev.Description,
            DueDate = ev.StartDate.Date,
            Status = Domain.Models.TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Domain.Models.Task? FindMatchingTask(
        IEnumerable<Domain.Models.Task> tasks,
        CalendarEvent ev)
    {
        if (ev.LinkedTaskId.HasValue)
            return tasks.FirstOrDefault(t => t.Id == ev.LinkedTaskId.Value);

        if (!string.IsNullOrEmpty(ev.ExternalUid))
        {
            // Try matching uid format "task-{guid}@notion-task-sync"
            var prefix = "task-";
            var suffix = "@notion-task-sync";
            var uid = ev.ExternalUid;
            if (uid.StartsWith(prefix) && uid.EndsWith(suffix))
            {
                var guidPart = uid[prefix.Length..^suffix.Length];
                if (Guid.TryParse(guidPart, out var taskId))
                    return tasks.FirstOrDefault(t => t.Id == taskId);
            }
        }

        return tasks.FirstOrDefault(t => t.Title.Equals(ev.Title, StringComparison.OrdinalIgnoreCase));
    }

    // -------------------------------------------------------------------------
    // iCal text / date helpers
    // -------------------------------------------------------------------------

    private static string EscapeICalText(string text) =>
        text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");

    private static string UnescapeICalText(string text) =>
        text.Replace("\\n", "\n").Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\");

    private static DateTime ParseICalDate(string line)
    {
        // Handles "DTSTART;VALUE=DATE:20260101" and "DTSTART:20260101T000000Z"
        var colonIdx = line.LastIndexOf(':');
        if (colonIdx < 0) return DateTime.UtcNow;
        var value = line[(colonIdx + 1)..].Trim();

        if (value.Length >= 8 && DateTime.TryParseExact(
                value[..8], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParseExact(
                value, "yyyyMMddTHHmmssZ",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dateTime))
            return dateTime;

        return DateTime.UtcNow;
    }
}
