// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Workers;

using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Events;

/// <summary>
/// Background worker that periodically synchronizes tasks between Notion and local storage.
/// Runs on a configurable interval and handles failures gracefully with backoff.
/// Essential for keeping data in sync without requiring manual intervention.
/// </summary>
public class SyncWorker : IDisposable
{
    private readonly SyncService _syncService;
    private readonly EventBus _eventBus;
    private readonly ILogger<SyncWorker> _logger;
    private readonly SyncConfig _config;
    private readonly int _syncIntervalSeconds;

    private CancellationTokenSource? _cancellationTokenSource;
    private global::System.Threading.Tasks.Task? _workerTask;
    private bool _isRunning;

    public SyncWorker(
        SyncService syncService,
        EventBus eventBus,
        ILogger<SyncWorker> logger,
        SyncConfig config,
        int syncIntervalSeconds = 300)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _syncIntervalSeconds = Math.Max(syncIntervalSeconds, 60); // Minimum 60 seconds
    }

    /// <summary>
    /// Starts the background sync worker.
    /// Returns immediately; worker runs in background thread.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Sync worker is already running");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = true;

        _workerTask = global::System.Threading.Tasks.Task.Run(async () => await RunWorkerAsync(_cancellationTokenSource.Token));

        _logger.LogInformation("Sync worker started (interval: {Interval}s)", _syncIntervalSeconds);
    }

    /// <summary>
    /// Stops the background sync worker.
    /// Waits for current sync operation to complete before returning.
    /// </summary>
    public async global::System.Threading.Tasks.Task StopAsync()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Sync worker is not running");
            return;
        }

        _logger.LogInformation("Stopping sync worker...");

        _cancellationTokenSource?.Cancel();
        _isRunning = false;

        if (_workerTask != null)
        {
            try
            {
                await _workerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is used
                _logger.LogDebug("Sync worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping sync worker");
            }
        }

        _logger.LogInformation("Sync worker stopped");
    }

    /// <summary>
    /// Main worker loop that executes sync operations at configured intervals.
    /// </summary>
    private async global::System.Threading.Tasks.Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        var failureCount = 0;
        const int maxConsecutiveFailures = 3;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Publish sync started event
                await _eventBus.PublishAsync(new SyncStartedEvent
                {
                    SyncConfigId = _config.Id.ToString(),
                    DatabaseId = _config.NotionDatabaseId,
                    Source = nameof(SyncWorker)
                });

                // Execute sync
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await _syncService.ExecuteSyncAsync(_config);
                stopwatch.Stop();

                // Publish completion event
                await _eventBus.PublishAsync(new SyncCompletedEvent
                {
                    SyncConfigId = _config.Id.ToString(),
                    TasksProcessed = result.LocalTaskCount,
                    ChangesDetected = result.LocalChangesDetected + result.NotionChangesDetected,
                    ConflictsResolved = result.ConflictsResolved,
                    Duration = stopwatch.Elapsed,
                    Success = result.Status == Domain.Enums.SyncStatus.Completed,
                    ErrorMessage = result.ErrorMessage,
                    Source = nameof(SyncWorker)
                });

                // Reset failure count on success
                if (result.Status == Domain.Enums.SyncStatus.Completed)
                {
                    failureCount = 0;
                    _logger.LogInformation("Sync completed successfully. Next sync in {Interval}s", _syncIntervalSeconds);
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning("Sync completed with errors: {Error}", result.ErrorMessage);
                }

                // Check if too many consecutive failures
                if (failureCount >= maxConsecutiveFailures)
                {
                    _logger.LogError("Too many consecutive sync failures. Stopping worker.");
                    await StopAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Unexpected error in sync worker ({FailureCount}/{MaxFailures})",
                    failureCount, maxConsecutiveFailures);

                if (failureCount >= maxConsecutiveFailures)
                {
                    _logger.LogError("Too many consecutive failures. Stopping worker.");
                    await StopAsync();
                    return;
                }
            }

            // Wait for next sync interval
            try
            {
                await global::System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(_syncIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
        }

        _logger.LogInformation("Sync worker loop ended");
    }

    /// <summary>
    /// Gets the current running state of the worker.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Cleans up resources when worker is disposed.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
