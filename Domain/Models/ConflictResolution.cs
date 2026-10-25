#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a conflict detected during sync and its resolution outcome.
/// Tracks both local and Notion versions for audit and manual review.
/// </summary>
public class ConflictResolution
{
    /// <summary>
    /// Unique identifier of this conflict record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the task this conflict belongs to.
    /// </summary>
    [Required]
    public required Guid TaskId { get; set; }

    /// <summary>
    /// Category of the conflict.
    /// </summary>
    public ConflictType ConflictType { get; set; }

    /// <summary>
    /// Name of the property involved in the conflict, or null when the conflict is not tied to a single property.
    /// </summary>
    [StringLength(200)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Value from the local task file, as a string.
    /// </summary>
    [StringLength(1000)]
    public string? LocalValue { get; set; }

    /// <summary>
    /// Value from the Notion page, as a string.
    /// </summary>
    [StringLength(1000)]
    public string? NotionValue { get; set; }

    /// <summary>
    /// Value chosen as the outcome of the resolution, or null if not yet resolved.
    /// </summary>
    [StringLength(1000)]
    public string? ResolvedValue { get; set; }

    /// <summary>
    /// Method used to resolve the conflict.
    /// </summary>
    public ResolutionMethod ResolutionMethod { get; set; }

    /// <summary>
    /// UTC time the conflict was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC time the conflict was resolved, or null if it has not been resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Current lifecycle status of the conflict.
    /// </summary>
    public ResolutionStatus Status { get; set; } = ResolutionStatus.Pending;

    /// <summary>
    /// Timestamp of the local modification involved in this conflict.
    /// Used by last-write-wins strategy to determine which value is authoritative.
    /// </summary>
    public DateTime? LocalModifiedAt { get; set; }

    /// <summary>
    /// Timestamp of the Notion modification involved in this conflict.
    /// Used by last-write-wins strategy to determine which value is authoritative.
    /// </summary>
    public DateTime? NotionModifiedAt { get; set; }

    /// <summary>
    /// Free-form notes about how the conflict was resolved, or why it was flagged for manual review.
    /// </summary>
    [StringLength(500)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Identifier of the user or system component that resolved the conflict.
    /// </summary>
    [StringLength(256)]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Validates the conflict resolution ensuring values are populated for the conflict type.
    /// </summary>
    public bool Validate()
    {
        if (TaskId == Guid.Empty)
            return false;

        if (ConflictType == ConflictType.Unknown)
            return false;

        if (Status == ResolutionStatus.Resolved && string.IsNullOrEmpty(ResolvedValue))
            return false;

        return true;
    }

    /// <summary>
    /// Marks the conflict as resolved using a specific method and value.
    /// </summary>
    public void Resolve(string resolvedValue, ResolutionMethod method, string? notes = null)
    {
        ResolvedValue = resolvedValue;
        ResolutionMethod = method;
        Status = ResolutionStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(notes))
            ResolutionNotes = notes;
    }

    /// <summary>
    /// Marks the conflict for manual review by an administrator.
    /// </summary>
    public void MarkForManualReview(string reason)
    {
        Status = ResolutionStatus.PendingReview;
        ResolutionMethod = ResolutionMethod.Manual;
        ResolutionNotes = reason;
    }

    /// <summary>
    /// Gets a summary string describing the conflict.
    /// </summary>
    public string GetConflictSummary()
    {
        return $"Conflict: {ConflictType} - Local: {LocalValue}, Notion: {NotionValue}";
    }

    /// <summary>
    /// Calculates the age of the conflict since detection.
    /// </summary>
    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - DetectedAt;
    }

    /// <summary>
    /// Determines if the conflict is still pending resolution.
    /// </summary>
    public bool IsPending()
    {
        return Status == ResolutionStatus.Pending || Status == ResolutionStatus.PendingReview;
    }
}

/// <summary>
/// Categories of conflicts that can be detected during sync.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Conflict type could not be determined. Treated as invalid by <see cref="ConflictResolution.Validate"/>.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Both the local and Notion versions were modified since the last sync.
    /// </summary>
    ConcurrentModification = 1,

    /// <summary>
    /// The task was deleted on one side while it was changed on the other.
    /// </summary>
    DeletionConflict = 2,

    /// <summary>
    /// A property has different values on the local and Notion sides.
    /// </summary>
    PropertyMismatch = 3,

    /// <summary>
    /// The conflict was caused by a validation failure.
    /// </summary>
    ValidationError = 4
}

/// <summary>
/// Strategies used to settle a conflict.
/// </summary>
public enum ResolutionMethod
{
    /// <summary>
    /// The change with the most recent modification timestamp wins.
    /// </summary>
    LastWrite = 0,

    /// <summary>
    /// The local value is kept.
    /// </summary>
    LocalWins = 1,

    /// <summary>
    /// The Notion value is kept.
    /// </summary>
    NotionWins = 2,

    /// <summary>
    /// The local and Notion values are combined into a new value.
    /// </summary>
    Merged = 3,

    /// <summary>
    /// The conflict is settled by a person.
    /// </summary>
    Manual = 4
}

/// <summary>
/// Lifecycle states of a conflict resolution.
/// </summary>
public enum ResolutionStatus
{
    /// <summary>
    /// The conflict has been detected and not yet resolved.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The conflict is waiting for manual review by an administrator.
    /// </summary>
    PendingReview = 1,

    /// <summary>
    /// The conflict has been resolved and a resolved value is set.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// The conflict was dropped without being resolved.
    /// </summary>
    Abandoned = 3
}
