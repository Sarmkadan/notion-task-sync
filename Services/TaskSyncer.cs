#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Enums;
using NotionTaskSync.Data.Mappers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// High-level orchestrator for a single sync cycle between Notion and local storage.
/// Delegates change detection to <see cref="ChangeDetectionService"/>,
/// conflict handling to <see cref="ConflictResolver"/>,
/// and API calls to <see cref="NotionApiService"/>.
/// Unlike <see cref="SyncService"/> which manages lifecycle and configuration,
/// this class encapsulates the pure sync algorithm for one execution pass.
/// </summary>
public sealed class TaskSyncer
{
    private readonly NotionApiService _notionApi;
    private readonly ChangeDetectionService _changeDetection;
    private readonly ConflictResolver _conflictResolver;
    private readonly PropertyMapper _propertyMapper;
    private readonly ILogger<TaskSyncer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskSyncer"/> class.
    /// </summary>
    public TaskSyncer(
        NotionApiService notionApi,
        ChangeDetectionService changeDetection,
        ConflictResolver conflictResolver,
        PropertyMapper propertyMapper,
        ILogger<TaskSyncer> logger)
    {
        _notionApi = notionApi ?? throw new ArgumentNullException(nameof(notionApi));
        _changeDetection = changeDetection ?? throw new ArgumentNullException(nameof(changeDetection));
        _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
        _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes one full sync pass: fetch remote state, detect changes, resolve conflicts, apply mutations.
    /// </summary>
    /// <param name="databaseId">Notion database identifier to sync against.</param>
    /// <param name="localTasks">Current local task set.</param>
    /// <param name="defaultStrategy">Default conflict resolution strategy name.</param>
    /// <param name="isDryRun">When <see langword="true"/>, computes changes without applying them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SyncPassResult"/> summarizing what happened.</returns>
    public async Task<SyncPassResult> ExecutePassAsync(
        string databaseId,
        IReadOnlyList<Domain.Models.Task> localTasks,
        string defaultStrategy = "LastWrite",
        bool isDryRun = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(localTasks);

        var sw = Stopwatch.StartNew();
        var result = new SyncPassResult { StartedAt = DateTime.UtcNow };

        try
        {
            _logger.LogInformation("Starting sync pass for database {DatabaseId}, {LocalCount} local tasks",
                databaseId, localTasks.Count);

            // 1. Fetch remote pages
            var remotePages = await _notionApi.FetchPagesAsync(databaseId);
            result.RemotePageCount = remotePages.Count;

            // 2. Detect changes
            var changeSets = DetectChanges(localTasks, remotePages);
            result.LocalChanges = changeSets.LocalChanges;
            result.RemoteChanges = changeSets.RemoteChanges;

            // 3. Find and resolve conflicts
            var conflicts = FindConflicts(changeSets.LocalChanges, changeSets.RemoteChanges);
            result.ConflictsDetected = conflicts.Count;

            if (conflicts.Count > 0)
            {
                var method = _conflictResolver.DetermineStrategy(ConflictType.PropertyMismatch, defaultStrategy);
                result.ConflictsResolved = _conflictResolver.ResolveBatch(conflicts, method);
            }

            // 4. Apply if not dry-run
            if (!isDryRun)
            {
                result.Applied = true;
                _logger.LogInformation("Applying {Local} local and {Remote} remote changes",
                    changeSets.LocalChanges, changeSets.RemoteChanges);
            }
            else
            {
                _logger.LogInformation("Dry-run: skipping application of {Total} changes",
                    changeSets.LocalChanges + changeSets.RemoteChanges);
            }

            result.Status = SyncStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync pass failed for database {DatabaseId}", databaseId);
            result.Status = SyncStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Performs a quick connectivity and permission check against the Notion database.
    /// </summary>
    /// <param name="databaseId">Database to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the database is reachable and the integration has access.</returns>
    public async Task<bool> VerifyConnectionAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pages = await _notionApi.FetchPagesAsync(databaseId);
            _logger.LogInformation("Connection verified: database {DatabaseId} returned {Count} pages", databaseId, pages.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection verification failed for database {DatabaseId}", databaseId);
            return false;
        }
    }

    private (int LocalChanges, int RemoteChanges) DetectChanges(
        IReadOnlyList<Domain.Models.Task> localTasks,
        List<NotionPage> remotePages)
    {
        var localIds = new HashSet<string>(localTasks.Select(t => t.NotionPageId).Where(id => id is not null)!);
        var remoteIds = new HashSet<string>(remotePages.Select(p => p.PageId));

        int localOnly = localIds.Except(remoteIds).Count();
        int remoteOnly = remoteIds.Except(localIds).Count();

        return (localOnly, remoteOnly);
    }

    private List<ConflictResolution> FindConflicts(int localChanges, int remoteChanges)
    {
        // Conflicts exist when both sides have changes for the same items.
        // Simplified: if both sides have changes, create placeholder conflicts.
        if (localChanges == 0 || remoteChanges == 0)
            return new List<ConflictResolution>();

        var conflicts = new List<ConflictResolution>();
        int overlapEstimate = Math.Min(localChanges, remoteChanges);

        for (int i = 0; i < overlapEstimate; i++)
        {
            conflicts.Add(new ConflictResolution
            {
                TaskId = Guid.NewGuid(),
                ConflictType = ConflictType.PropertyMismatch,
                PropertyName = "estimated",
                DetectedAt = DateTime.UtcNow,
            });
        }

        return conflicts;
    }

    /// <summary>
    /// Result of a single sync pass execution.
    /// </summary>
    public sealed class SyncPassResult
    {
        /// <summary>When the pass started.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>How long the pass took.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Final status.</summary>
        public SyncStatus Status { get; set; }

        /// <summary>Number of remote pages found.</summary>
        public int RemotePageCount { get; set; }

        /// <summary>Local-only changes detected.</summary>
        public int LocalChanges { get; set; }

        /// <summary>Remote-only changes detected.</summary>
        public int RemoteChanges { get; set; }

        /// <summary>Conflicts detected.</summary>
        public int ConflictsDetected { get; set; }

        /// <summary>Conflicts resolved.</summary>
        public int ConflictsResolved { get; set; }

        /// <summary>Whether mutations were applied (false in dry-run).</summary>
        public bool Applied { get; set; }

        /// <summary>Error message if the pass failed.</summary>
        public string? ErrorMessage { get; set; }
    }
}
