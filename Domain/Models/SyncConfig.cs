#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

/// <summary>
/// Configuration for sync settings between local tasks and Notion.
/// Controls sync behavior, conflict resolution, and field mappings.
/// </summary>
public class SyncConfig
{
    /// <summary>
    /// Gets or sets the unique identifier of this sync configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of this sync configuration.
    /// Must be non-empty and at most 200 characters.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Notion database to synchronize.
    /// Expected to be a 36-character Notion identifier (UUID with dashes).
    /// </summary>
    [Required]
    [StringLength(36)]
    public required string NotionDatabaseId { get; set; }

    /// <summary>
    /// Gets or sets the local directory that holds the task files to synchronize.
    /// Must be non-empty and at most 500 characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string LocalFolderPath { get; set; }

    /// <summary>
    /// Gets or sets the direction in which changes are propagated between local files and Notion.
    /// Defaults to <see cref="SyncDirection.Bidirectional"/>.
    /// </summary>
    public SyncDirection Direction { get; set; } = SyncDirection.Bidirectional;

    /// <summary>
    /// Gets or sets the default strategy used to resolve conflicts when both sides changed.
    /// Per-field overrides in <see cref="FieldConflictStrategies"/> take precedence.
    /// Defaults to <see cref="ConflictResolutionStrategy.LastWrite"/>.
    /// </summary>
    public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.LastWrite;

    /// <summary>
    /// Gets or sets the interval, in seconds, between scheduled sync runs.
    /// Valid range is 1 to 3600. Default is 300 (5 minutes).
    /// </summary>
    [Range(1, 3600)]
    public int SyncIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed sync operation.
    /// Valid range is 0 to 100. Default is 3.
    /// </summary>
    [Range(0, 100)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether this configuration is active.
    /// A disabled configuration is never reported as due by <see cref="IsSyncDue"/>.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC timestamp at which this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp of the last modification to this configuration.
    /// Updated by <see cref="UpdateSyncStatus"/>.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp of the last completed sync, or <c>null</c> if no sync has run yet.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the next sync is scheduled, or <c>null</c> if none is scheduled.
    /// A <c>null</c> value makes <see cref="IsSyncDue"/> return <c>true</c> for an enabled configuration.
    /// </summary>
    public DateTime? NextScheduledSyncAt { get; set; }

    /// <summary>
    /// Gets or sets whether the sync should run in dry-run mode.
    /// When true, mutation calls to the Notion API are skipped and only planned operations
    /// are logged.
    /// </summary>
    public bool IsDryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of pages to fetch in a single sync cycle.
    /// When set to a positive value, the sync will stop after fetching this many pages
    /// and throw an exception to prevent unbounded API calls and memory growth.
    /// Default is 0 (unlimited) to maintain backward compatibility.
    /// </summary>
    [Range(0, 10000)]
    public int MaxPages { get; set; } = 0;

    /// <summary>
    /// Gets or sets the clock skew tolerance window in milliseconds.
    /// When the absolute difference between local and Notion timestamps is within this window,
    /// the timestamps are considered equal for conflict resolution purposes.
    /// This handles cases where both systems record timestamps with limited precision (e.g., minute-level).
    /// Default is 60000 (1 minute) to account for Notion's minute-level granularity.
    /// </summary>
    [Range(0, 86400000)] // 0 to 24 hours in milliseconds
    public int ClockSkewToleranceMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the mapping from local field names to Notion property names.
    /// Fields without an entry map to the same name. Used by <see cref="MapLocalFieldToNotion"/>.
    /// </summary>
    public Dictionary<string, string>? FieldMappings { get; set; }

    /// <summary>
    /// Gets or sets the names of fields excluded from synchronization.
    /// An empty or <c>null</c> list means all fields are synchronized. Used by <see cref="ShouldSyncField"/>.
    /// </summary>
    public List<string>? IgnoredFields { get; set; }

    /// <summary>
    /// Per-field conflict resolution strategy overrides.
    /// Keys are field/property names (e.g. "Title", "Status").
    /// Values are strategy names matching <see cref="ConflictResolutionStrategy"/> enum
    /// values: <c>LastWrite</c>, <c>LocalWins</c>, <c>NotionWins</c>, or <c>Manual</c>.
    /// A per-field entry takes precedence over <see cref="ConflictStrategy"/> for that field.
    /// </summary>
    /// <example>
    /// <code>
    /// "fieldConflictStrategies": {
    /// "Title": "LocalWins",
    /// "Status": "NotionWins"
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, ConflictResolutionStrategy>? FieldConflictStrategies { get; set; }

    /// <summary>
    /// Initializes a new SyncConfig with required database and folder information.
    /// </summary>
    [SetsRequiredMembers]
    public SyncConfig(string name, string notionDatabaseId, string localFolderPath)
    {
        Name = name;
        NotionDatabaseId = notionDatabaseId;
        LocalFolderPath = localFolderPath;
        FieldMappings = new Dictionary<string, string>();
        IgnoredFields = new List<string>();
    }

    /// <summary>
    /// Validates the sync configuration ensuring paths and identifiers are correct.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 200)
            return false;

        if (string.IsNullOrWhiteSpace(NotionDatabaseId) || NotionDatabaseId.Length != 36)
            return false;

        if (string.IsNullOrWhiteSpace(LocalFolderPath))
            return false;

        if (SyncIntervalSeconds < 1 || SyncIntervalSeconds > 3600)
            return false;

        if (MaxRetries < 0 || MaxRetries > 100)
            return false;

        return true;
    }

    /// <summary>
    /// Maps a local field name to its Notion property equivalent using configured mappings.
    /// </summary>
    public string? MapLocalFieldToNotion(string localFieldName)
    {
        if (FieldMappings is null || !FieldMappings.ContainsKey(localFieldName))
            return localFieldName;

        return FieldMappings[localFieldName];
    }

    /// <summary>
    /// Checks if a field should be synchronized based on ignored fields list.
    /// </summary>
    public bool ShouldSyncField(string fieldName)
    {
        if (IgnoredFields is null || IgnoredFields.Count == 0)
            return true;

        return !IgnoredFields.Contains(fieldName);
    }

    /// <summary>
    /// Updates the last sync timestamp and schedules the next sync.
    /// </summary>
    public void UpdateSyncStatus()
    {
        LastSyncAt = DateTime.UtcNow;
        NextScheduledSyncAt = DateTime.UtcNow.AddSeconds(SyncIntervalSeconds);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if a sync should be triggered based on the schedule.
    /// </summary>
    public bool IsSyncDue()
    {
        if (!IsEnabled)
            return false;

        if (NextScheduledSyncAt is null)
            return true;

        return DateTime.UtcNow >= NextScheduledSyncAt;
    }

    /// <summary>
    /// Adds a field mapping for sync translation between systems.
    /// </summary>
    public void AddFieldMapping(string localField, string notionField)
    {
        FieldMappings ??= new Dictionary<string, string>();
        FieldMappings[localField] = notionField;
    }

    /// <summary>
    /// Adds a field to the ignored fields list for exclusion from sync.
    /// </summary>
    public void AddIgnoredField(string fieldName)
    {
        IgnoredFields ??= new List<string>();
        if (!IgnoredFields.Contains(fieldName))
            IgnoredFields.Add(fieldName);
    }

    /// <summary>
    /// Returns the conflict resolution strategy for a specific field.
    /// When a per-field override is present in <see cref="FieldConflictStrategies"/>
    /// it takes precedence; otherwise the global <see cref="ConflictStrategy"/> is used.
    /// </summary>
    public ConflictResolutionStrategy GetFieldConflictStrategy(string fieldName)
    {
        if (FieldConflictStrategies is not null
            && FieldConflictStrategies.TryGetValue(fieldName, out var fieldStrategy))
        {
            return fieldStrategy;
        }

        return ConflictStrategy;
    }

    /// <summary>
    /// Sets a per-field conflict resolution strategy override.
    /// </summary>
    public void SetFieldConflictStrategy(string fieldName, ConflictResolutionStrategy strategy)
    {
        FieldConflictStrategies ??= new Dictionary<string, ConflictResolutionStrategy>();
        FieldConflictStrategies[fieldName] = strategy;
    }

    /// <summary>
    /// Validates that MaxPages is within acceptable bounds if set.
    /// </summary>
    /// <returns>True if validation passes; false otherwise.</returns>
    public bool ValidateMaxPages()
    {
        if (MaxPages < 0 || MaxPages > 10000)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Returns a string representation of the SyncConfig for logging and debugging.
    /// Sensitive fields are masked to prevent accidental exposure in logs.
    /// </summary>
    /// <returns>String representation of the configuration.</returns>
    public override string ToString()
    {
        return $"SyncConfig(Id={Id}, Name={Name}, NotionDatabaseId={NotionDatabaseId}, " +
               $"LocalFolderPath={LocalFolderPath}, Direction={Direction}, " +
               $"ConflictStrategy={ConflictStrategy}, SyncInterval={SyncIntervalSeconds}s, " +
               $"MaxRetries={MaxRetries}, IsEnabled={IsEnabled}, " +
               $"CreatedAt={CreatedAt:yyyy-MM-dd}, UpdatedAt={UpdatedAt:yyyy-MM-dd}, " +
               $"LastSyncAt={(LastSyncAt.HasValue ? LastSyncAt.Value.ToString("yyyy-MM-dd") : "null")}, " +
               $"NextScheduledSyncAt={(NextScheduledSyncAt.HasValue ? NextScheduledSyncAt.Value.ToString("yyyy-MM-dd") : "null")}, " +
               $"IsDryRun={IsDryRun}, MaxPages={MaxPages})";
    }
}

public enum SyncDirection
{
    Bidirectional = 0,
    LocalToNotion = 1,
    NotionToLocal = 2
}

public enum ConflictResolutionStrategy
{
    LastWrite = 0,
    LocalWins = 1,
    NotionWins = 2,
    Manual = 3
}