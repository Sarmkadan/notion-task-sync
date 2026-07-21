#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Configuration for sync settings between local tasks and Notion.
/// Controls sync behavior, conflict resolution, and field mappings.
/// </summary>
public class SyncConfig
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [Required]
    [StringLength(36)]
    public required string NotionDatabaseId { get; set; }

    [Required]
    [StringLength(500)]
    public required string LocalFolderPath { get; set; }

    [StringLength(256)]
    public string? NotionApiKey { get; set; }

    public SyncDirection Direction { get; set; } = SyncDirection.Bidirectional;

    public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.LastWrite;

    [Range(1, 3600)]
    public int SyncIntervalSeconds { get; set; } = 300;

    [Range(0, 100)]
    public int MaxRetries { get; set; } = 3;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSyncAt { get; set; }

    public DateTime? NextScheduledSyncAt { get; set; }

    /// <summary>
    /// Gets or sets whether the sync should run in dry-run mode.
    /// When true, mutation calls to the Notion API are skipped and only planned operations
    /// are logged.
    /// </summary>
    public bool IsDryRun { get; set; } = false;

    public Dictionary<string, string>? FieldMappings { get; set; }

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
