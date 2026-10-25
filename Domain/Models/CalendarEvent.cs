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
    /// <summary>
    /// Unique identifier of the event. Generated automatically when the instance is created.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable title of the event. Required, up to 500 characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    /// <summary>
    /// Optional free-text description of the event, up to 5000 characters.
    /// </summary>
    [StringLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// Start of the event. For all-day events, only the date portion is meaningful.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End of the event, or null when the event has no end date.
    /// Must not be earlier than <see cref="StartDate"/> for the event to pass <see cref="Validate"/>.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Indicates whether the event spans the whole day rather than a specific time range.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Optional location of the event, up to 500 characters.
    /// </summary>
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

    /// <summary>
    /// UTC timestamp of when the event was created. Set to the current UTC time by default.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last modification to the event. Set to the current UTC time by default.
    /// </summary>
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
