// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Observers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Events;
using NotionTaskSync.Models;

/// <summary>
/// Observer pattern implementation for monitoring sync operations.
/// Listens to sync events and maintains statistics about sync health and performance.
/// Provides real-time observability into the sync pipeline.
/// </summary>
public class SyncObserver
{
    private readonly EventBus _eventBus;
    private readonly ILogger<SyncObserver> _logger;
    private readonly SyncStatistics _statistics;

    public SyncObserver(EventBus eventBus, ILogger<SyncObserver> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statistics = new SyncStatistics();

        // Subscribe to all sync events
        RegisterEventHandlers();
    }

    /// <summary>
    /// Registers handlers for all sync events.
    /// </summary>
    private void RegisterEventHandlers()
    {
        _eventBus.Subscribe<SyncStartedEvent>(HandleSyncStarted);
        _eventBus.Subscribe<SyncCompletedEvent>(HandleSyncCompleted);
        _eventBus.Subscribe<ConflictDetectedEvent>(HandleConflictDetected);
        _eventBus.Subscribe<ChangeDetectedEvent>(HandleChangeDetected);
        _eventBus.Subscribe<TaskSynchronizedEvent>(HandleTaskSynchronized);
        _eventBus.Subscribe<RateLimitWarningEvent>(HandleRateLimitWarning);
        _eventBus.Subscribe<ValidationFailedEvent>(HandleValidationFailed);

        _logger.LogInformation("Sync observer registered for event monitoring");
    }

    /// <summary>
    /// Handles sync started event.
    /// Logs when sync operations begin.
    /// </summary>
    private async Task HandleSyncStarted(SyncStartedEvent @event)
    {
        _logger.LogInformation("Sync operation started for database: {DatabaseId}",
            @event.DatabaseId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles sync completed event.
    /// Records statistics about the completed sync.
    /// </summary>
    private async Task HandleSyncCompleted(SyncCompletedEvent @event)
    {
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

        var statusEmoji = @event.Success ? "✓" : "✗";
        _logger.LogInformation(
            "{Status} Sync completed - Tasks: {TaskCount}, Changes: {ChangeCount}, Conflicts Resolved: {ConflictCount}, Duration: {Duration}ms",
            statusEmoji,
            @event.TasksProcessed,
            @event.ChangesDetected,
            @event.ConflictsResolved,
            @event.Duration.TotalMilliseconds);

        // Log if sync had issues
        if (!@event.Success && !string.IsNullOrEmpty(@event.ErrorMessage))
        {
            _logger.LogError("Sync failed: {ErrorMessage}", @event.ErrorMessage);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles conflict detected event.
    /// Logs conflicts for manual review if needed.
    /// </summary>
    private async Task HandleConflictDetected(ConflictDetectedEvent @event)
    {
        _logger.LogWarning(
            "Conflict detected - Task: {TaskTitle}, Type: {ConflictType}, Local: {LocalTime}, Remote: {RemoteTime}",
            @event.TaskTitle,
            @event.ConflictType,
            @event.LocalModifiedAt,
            @event.RemoteModifiedAt);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles change detected event.
    /// Logs detected changes from either source.
    /// </summary>
    private async Task HandleChangeDetected(ChangeDetectedEvent @event)
    {
        _logger.LogDebug(
            "Change detected - Task: {TaskId}, Type: {ChangeType}, Source: {Source}, Properties: {PropertyCount}",
            @event.TaskId,
            @event.ChangeType,
            @event.Source,
            @event.ChangedProperties.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles task synchronized event.
    /// Tracks individual task synchronization results.
    /// </summary>
    private async Task HandleTaskSynchronized(TaskSynchronizedEvent @event)
    {
        if (@event.Successful)
        {
            _logger.LogDebug("Task synchronized successfully: {TaskTitle}", @event.TaskTitle);
        }
        else
        {
            _logger.LogWarning("Failed to synchronize task {TaskTitle}: {Error}",
                @event.TaskTitle,
                @event.ErrorMessage);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles rate limit warning event.
    /// Alerts when API rate limits are approaching.
    /// </summary>
    private async Task HandleRateLimitWarning(RateLimitWarningEvent @event)
    {
        _logger.LogWarning(
            "Rate limit warning for {ApiService}: {RequestsRemaining}/{RequestLimit} requests remaining. Resets at {ResetTime}",
            @event.ApiService,
            @event.RequestsRemaining,
            @event.RequestLimit,
            @event.ResetTime);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles validation failed event.
    /// Logs validation errors that occurred during sync.
    /// </summary>
    private async Task HandleValidationFailed(ValidationFailedEvent @event)
    {
        _logger.LogError(
            "Validation failed - Type: {ValidationType}, Errors: {ErrorCount}",
            @event.ValidationType,
            @event.ErrorMessages.Count);

        foreach (var error in @event.ErrorMessages)
        {
            _logger.LogError("  - {Error}", error);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets current sync statistics.
    /// </summary>
    public SyncStatistics GetStatistics() => _statistics;

    /// <summary>
    /// Resets all recorded statistics.
    /// </summary>
    public void ResetStatistics()
    {
        _statistics.Reset();
        _logger.LogInformation("Sync statistics reset");
    }

    /// <summary>
    /// Gets a report of sync health status.
    /// </summary>
    public string GetHealthReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Sync Health Report ===");
        report.AppendLine(_statistics.ToString());
        report.AppendLine($"Observer active: {_eventBus.GetSubscriberCount<SyncCompletedEvent>()} event subscribers");
        return report.ToString();
    }
}
