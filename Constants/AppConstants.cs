// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Constants;

using System;
using System.Collections.Generic;

/// <summary>
/// Application-wide constants and magic strings.
/// </summary>
public static class AppConstants
{
    // Application metadata
    public const string ApplicationName = "Notion Task Sync";
    public const string ApplicationVersion = "1.0.0";
    public const string Author = "Vladyslav Zaiets";
    public const string AuthorUrl = "https://sarmkadan.com";
    public const string RepositoryUrl = "https://github.com/vladyslavzaiets/notion-task-sync";

    // File and directory constants
    public const string ConfigFileName = "appsettings.json";
    public const string LocalConfigFileName = "appsettings.local.json";
    public const string LogFileName = "notion-sync.log";
    public const string BackupDirectoryName = "backups";
    public const string TaskFileExtension = ".md";

    // Default values
    public const int DefaultSyncIntervalSeconds = 300; // 5 minutes
    public const int DefaultMaxRetries = 3;
    public const int DefaultApiTimeoutSeconds = 30;
    public const int DefaultPageSize = 100;
    public const int DefaultBackupRetentionDays = 30;

    // Sync related constants
    public const string SyncLockFileName = ".sync.lock";
    public const int SyncLockTimeoutSeconds = 3600; // 1 hour
    public const string LastSyncTimeFormat = "yyyy-MM-dd HH:mm:ss";

    // Change tracking constants
    public const string ChangeTypeCreated = "Created";
    public const string ChangeTypeUpdated = "Updated";
    public const string ChangeTypeDeleted = "Deleted";
    public const string ChangeTypeSynced = "Synced";

    // Conflict resolution strategies
    public const string ConflictStrategyLastWrite = "LastWrite";
    public const string ConflictStrategyLocalWins = "LocalWins";
    public const string ConflictStrategyNotionWins = "NotionWins";
    public const string ConflictStrategyManual = "Manual";

    // Sync directions
    public const string SyncDirectionBidirectional = "Bidirectional";
    public const string SyncDirectionLocalToNotion = "LocalToNotion";
    public const string SyncDirectionNotionToLocal = "NotionToLocal";

    // Task status names
    public const string TaskStatusTodo = "Todo";
    public const string TaskStatusInProgress = "In Progress";
    public const string TaskStatusDone = "Done";
    public const string TaskStatusBlocked = "Blocked";
    public const string TaskStatusArchived = "Archived";

    // Property names used in Notion integration
    public const string NotionPropertyTitle = "Title";
    public const string NotionPropertyDescription = "Description";
    public const string NotionPropertyStatus = "Status";
    public const string NotionPropertyPriority = "Priority";
    public const string NotionPropertyDueDate = "Due Date";
    public const string NotionPropertyAssignee = "Assignee";
    public const string NotionPropertyCreatedTime = "Created";
    public const string NotionPropertyLastEditedTime = "Last Edited";

    // Validation constraints
    public const int MaxTaskTitleLength = 500;
    public const int MaxTaskDescriptionLength = 5000;
    public const int MaxPropertyNameLength = 100;
    public const int MaxPropertyValueLength = 1000;
    public const int MaxConfigNameLength = 200;
    public const int MaxPageIdLength = 36;

    // Priority levels
    public const int PriorityLowest = 0;
    public const int PriorityLow = 25;
    public const int PriorityMedium = 50;
    public const int PriorityHigh = 75;
    public const int PriorityHighest = 100;

    /// <summary>
    /// Gets all valid change types.
    /// </summary>
    public static readonly List<string> ValidChangeTypes = new()
    {
        ChangeTypeCreated,
        ChangeTypeUpdated,
        ChangeTypeDeleted,
        ChangeTypeSynced
    };

    /// <summary>
    /// Gets all valid sync directions.
    /// </summary>
    public static readonly List<string> ValidSyncDirections = new()
    {
        SyncDirectionBidirectional,
        SyncDirectionLocalToNotion,
        SyncDirectionNotionToLocal
    };

    /// <summary>
    /// Gets all valid conflict strategies.
    /// </summary>
    public static readonly List<string> ValidConflictStrategies = new()
    {
        ConflictStrategyLastWrite,
        ConflictStrategyLocalWins,
        ConflictStrategyNotionWins,
        ConflictStrategyManual
    };

    /// <summary>
    /// Gets all valid task statuses.
    /// </summary>
    public static readonly List<string> ValidTaskStatuses = new()
    {
        TaskStatusTodo,
        TaskStatusInProgress,
        TaskStatusDone,
        TaskStatusBlocked,
        TaskStatusArchived
    };
}
