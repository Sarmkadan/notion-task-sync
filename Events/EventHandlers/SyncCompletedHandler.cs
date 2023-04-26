#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events.EventHandlers;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Models;

/// <summary>
/// Event handler that responds to sync completion events.
/// Updates statistics, logs results, and triggers post-sync actions.
/// Provides visibility into sync operation outcomes and trends.
/// </summary>
public class SyncCompletedHandler
{
    private readonly ILogger<SyncCompletedHandler> _logger;
    private readonly SyncStatistics _statistics;

    public SyncCompletedHandler(
        ILogger<SyncCompletedHandler> logger,
        SyncStatistics statistics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }

    /// <summary>
    /// Handles a sync completed event.
    /// Records statistics and performs post-sync operations.
    /// </summary>
    public async Task HandleAsync(SyncCompletedEvent @event)
    {
        try
        {
            // Record the operation in statistics
            var snapshot = new SyncOperationSnapshot
            {
                Timestamp = @event.Timestamp,
                DurationMs = (long)@event.Duration.TotalMilliseconds,
                Successful = @event.Success,
                TasksProcessed = @event.TasksProcessed,
                ChangesDetected = @event.ChangesDetected,
                ConflictsDetected = @event.ConflictsResolved,
                ErrorMessage = @event.ErrorMessage
            };

            _statistics.RecordOperation(snapshot);

            // Log the result with appropriate level
            if (@event.Success)
            {
                _logger.LogInformation(
                    "✓ Sync completed successfully. Processed: {TaskCount} tasks, " +
                    "Changes: {ChangeCount}, Conflicts resolved: {ConflictCount}, Duration: {Duration}ms",
                    @event.TasksProcessed,
                    @event.ChangesDetected,
                    @event.ConflictsResolved,
                    @event.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "✗ Sync failed after {Duration}ms. Error: {ErrorMessage}",
                    @event.Duration.TotalMilliseconds,
                    @event.ErrorMessage);
            }

            // Check for warning conditions
            CheckForWarnings(@event);

            // Could trigger cleanup or maintenance actions
            await PerformPostSyncActionsAsync(@event).ConfigureAwait(false);

            _logger.LogDebug("Sync completed handler processed event {EventId}", @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling sync completed event");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks for conditions that warrant warnings.
    /// </summary>
    private void CheckForWarnings(SyncCompletedEvent @event)
    {
        // Warn if sync took too long
        if (@event.Duration.TotalSeconds > 60)
        {
            _logger.LogWarning("Sync operation took longer than expected: {Duration}s",
                @event.Duration.TotalSeconds);
        }

        // Warn if many conflicts
        if (@event.ConflictsResolved > 10)
        {
            _logger.LogWarning("High number of conflicts detected and resolved: {ConflictCount}",
                @event.ConflictsResolved);
        }

        // Warn if many changes
        if (@event.ChangesDetected > 100)
        {
            _logger.LogWarning("Large number of changes detected: {ChangeCount}",
                @event.ChangesDetected);
        }
    }

    /// <summary>
    /// Performs actions after sync completes.
    /// Placeholder for post-sync operations.
    /// </summary>
    private async Task PerformPostSyncActionsAsync(SyncCompletedEvent @event)
    {
        // TODO: Implement post-sync actions:
        // - Save statistics to persistent storage
        // - Send notifications
        // - Trigger cleanup
        // - Archive old change logs

        await Task.CompletedTask;
    }
}
