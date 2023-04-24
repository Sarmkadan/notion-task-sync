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
    public Guid Id { get; set; }

    [Required]
    public required Guid TaskId { get; set; }

    public ConflictType ConflictType { get; set; }

    [StringLength(200)]
    public string? PropertyName { get; set; }

    [StringLength(1000)]
    public string? LocalValue { get; set; }

    [StringLength(1000)]
    public string? NotionValue { get; set; }

    [StringLength(1000)]
    public string? ResolvedValue { get; set; }

    public ResolutionMethod ResolutionMethod { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public ResolutionStatus Status { get; set; } = ResolutionStatus.Pending;

    [StringLength(500)]
    public string? ResolutionNotes { get; set; }

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

public enum ConflictType
{
    Unknown = 0,
    ConcurrentModification = 1,
    DeletionConflict = 2,
    PropertyMismatch = 3,
    ValidationError = 4
}

public enum ResolutionMethod
{
    LastWrite = 0,
    LocalWins = 1,
    NotionWins = 2,
    Merged = 3,
    Manual = 4
}

public enum ResolutionStatus
{
    Pending = 0,
    PendingReview = 1,
    Resolved = 2,
    Abandoned = 3
}
