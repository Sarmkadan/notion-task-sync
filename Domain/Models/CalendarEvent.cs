#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a calendar event that can be synchronized with a task's due date and schedule.
/// Events can originate from external iCal sources or be generated from local tasks.
/// </summary>
public class CalendarEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    [StringLength(5000)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsAllDay { get; set; }

    [StringLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// The UID field from an iCal event, used for deduplication during import.
    /// </summary>
    [StringLength(256)]
    public string? ExternalUid { get; set; }

    /// <summary>
    /// Reference to the local task this event was generated from or synced to.
    /// Null when the event was imported from an external calendar and no matching task exists yet.
    /// </summary>
    public Guid? LinkedTaskId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether this event was generated from a local task or imported from a calendar file.
    /// </summary>
    public CalendarEventSource Source { get; set; } = CalendarEventSource.Task;

    /// <summary>
    /// Validates the event ensuring required fields are present and date ranges are valid.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 500)
            return false;

        if (EndDate.HasValue && EndDate.Value < StartDate)
            return false;

        return true;
    }

    /// <summary>
    /// Returns the event duration, or null for events without an end date.
    /// </summary>
    public TimeSpan? GetDuration() =>
        EndDate.HasValue ? EndDate.Value - StartDate : null;
}

/// <summary>
/// Indicates where a calendar event originated.
/// </summary>
public enum CalendarEventSource
{
    /// <summary>Event was generated from a local task's due date.</summary>
    Task = 0,
    /// <summary>Event was imported from an external iCal (.ics) file.</summary>
    Import = 1
}

/// <summary>
/// Result of a calendar sync operation detailing what was created, updated, or skipped.
/// </summary>
public class CalendarSyncResult
{
    public int EventsExported { get; set; }
    public int EventsImported { get; set; }
    public int TasksCreated { get; set; }
    public int TasksUpdated { get; set; }
    public int Skipped { get; set; }
    public List<string> Warnings { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
