// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Commands;

using NotionTaskSync.Cli;
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Executes a synchronization operation between Notion and local tasks.
/// Handles database selection, sync direction, and conflict resolution strategy.
/// This is the primary command users interact with for syncing data.
/// </summary>
public class SyncCommand : CliCommand
{
    private readonly SyncService _syncService;
    private readonly ILogger<SyncCommand> _logger;

    public override string Description => "Synchronize tasks between Notion and local files";

    public override Dictionary<string, string> Options => new()
    {
        { "database-id", "Notion database ID to sync with (required)" },
        { "direction", "Sync direction: bidirectional, local-to-notion, or notion-to-local (default: bidirectional)" },
        { "strategy", "Conflict resolution: last-write, manual, or local-priority (default: last-write)" },
        { "verbose", "Enable verbose logging output" },
        { "dry-run", "Preview changes without applying them" },
        { "backup", "Create backup before syncing (default: true)" }
    };

    public SyncCommand(SyncService syncService, ILogger<SyncCommand> logger)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the sync operation with the specified options.
    /// Validates configuration, initiates sync, and reports results.
    /// </summary>
    public override async Task<int> ExecuteAsync(List<string> arguments, Dictionary<string, string> options)
    {
        try
        {
            // Validate required options
            if (!options.ContainsKey("database-id") || string.IsNullOrWhiteSpace(options["database-id"]))
            {
                _logger.LogError("Missing required option: --database-id");
                return 1;
            }

            var databaseId = options["database-id"];
            var isDryRun = options.ContainsKey("dry-run") && options["dry-run"] == "true";
            var createBackup = !options.ContainsKey("backup") || options["backup"] != "false";

            _logger.LogInformation("Starting sync operation for database: {DatabaseId}", databaseId);

            // Create sync configuration
            var config = new SyncConfig(
                name: $"CLI Sync {DateTime.UtcNow:G}",
                notionDatabaseId: databaseId,
                localFolderPath: "./tasks"
            );

            // Apply direction preference
            if (options.ContainsKey("direction"))
            {
                config.Direction = ParseSyncDirection(options["direction"]);
            }

            // Apply conflict resolution strategy
            if (options.ContainsKey("strategy"))
            {
                config.ConflictStrategy = ParseConflictStrategy(options["strategy"]);
            }

            if (isDryRun)
            {
                _logger.LogInformation("Running in dry-run mode - no changes will be applied");
            }

            // Execute sync
            var result = await _syncService.ExecuteSyncAsync(config);

            // Log results
            LogSyncResults(result);

            return result.Status == Domain.Enums.SyncStatus.Completed ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync command execution failed");
            return 1;
        }
    }

    /// <summary>
    /// Parses sync direction string into enum value.
    /// Defaults to Bidirectional if invalid input.
    /// </summary>
    private Domain.Enums.SyncDirection ParseSyncDirection(string direction)
    {
        return direction?.ToLowerInvariant() switch
        {
            "local-to-notion" => Domain.Enums.SyncDirection.LocalToNotion,
            "notion-to-local" => Domain.Enums.SyncDirection.NotionToLocal,
            _ => Domain.Enums.SyncDirection.Bidirectional
        };
    }

    /// <summary>
    /// Parses conflict strategy string into enum value.
    /// Defaults to LastWrite if invalid input.
    /// </summary>
    private Domain.Enums.ConflictStrategy ParseConflictStrategy(string strategy)
    {
        return strategy?.ToLowerInvariant() switch
        {
            "manual" => Domain.Enums.ConflictStrategy.Manual,
            "local-priority" => Domain.Enums.ConflictStrategy.LocalPriority,
            _ => Domain.Enums.ConflictStrategy.LastWrite
        };
    }

    /// <summary>
    /// Logs sync operation results in a user-friendly format.
    /// </summary>
    private void LogSyncResults(SyncService.SyncResult result)
    {
        _logger.LogInformation("Sync Results:");
        _logger.LogInformation("  Status: {Status}", result.Status);
        _logger.LogInformation("  Local Tasks: {LocalCount}", result.LocalTaskCount);
        _logger.LogInformation("  Notion Pages: {NotionCount}", result.NotionPageCount);
        _logger.LogInformation("  Local Changes: {LocalChanges}", result.LocalChangesDetected);
        _logger.LogInformation("  Notion Changes: {NotionChanges}", result.NotionChangesDetected);
        _logger.LogInformation("  Conflicts: {ConflictCount}", result.ConflictsDetected);
        _logger.LogInformation("  Resolved: {ResolvedCount}", result.ConflictsResolved);
        _logger.LogInformation("  Duration: {Duration}ms", result.Duration?.TotalMilliseconds);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            _logger.LogError("  Error: {Error}", result.ErrorMessage);
        }
    }
}
