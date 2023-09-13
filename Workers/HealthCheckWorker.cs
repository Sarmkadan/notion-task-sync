#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Workers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background worker that monitors application health and resource usage.
/// Performs periodic health checks and logs warnings if issues are detected.
/// Helps identify memory leaks, connection issues, and performance degradation.
/// </summary>
public class HealthCheckWorker : IDisposable
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly int _checkIntervalSeconds;

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;
    private bool _isRunning;
    private long _memoryUsageMb;
    private int _threadCount;
    private long _uptimeSeconds;
    private DateTime _checkedAt;

    public HealthCheckWorker(ILogger<HealthCheckWorker> logger, int checkIntervalSeconds = 300)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkIntervalSeconds = Math.Max(checkIntervalSeconds, 60);
    }

    /// <summary>
    /// Starts the health check worker.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Health check worker is already running");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = true;

        _workerTask = Task.Run(async () => await RunHealthChecksAsync(_cancellationTokenSource.Token));

        _logger.LogInformation("Health check worker started (interval: {Interval}s)", _checkIntervalSeconds);
    }

    /// <summary>
    /// Stops the health check worker.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _logger.LogInformation("Stopping health check worker...");

        _cancellationTokenSource?.Cancel();
        _isRunning = false;

        if (_workerTask is not null)
        {
            try
            {
                await _workerTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Health check worker cancelled");
            }
        }

        _logger.LogInformation("Health check worker stopped");
    }

    /// <summary>
    /// Main health check loop.
    /// </summary>
    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Performs various health checks.
    /// </summary>
    private async Task PerformHealthChecksAsync()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        _memoryUsageMb = process.WorkingSet64 / (1024 * 1024);
        _threadCount = process.Threads.Count;
        _uptimeSeconds = (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds;
        _checkedAt = DateTime.UtcNow;

        // Check memory usage
        CheckMemoryUsage(_memoryUsageMb);

        // Check thread count
        CheckThreadCount(_threadCount);

        // Check connectivity (would require actual implementation)
        await CheckConnectivityAsync();

        _logger.LogDebug("Health check completed");
    }

    /// <summary>
    /// Checks current memory usage and logs warnings if too high.
    /// </summary>
    /// <param name="memoryMb">The memory usage in megabytes.</param>
    private void CheckMemoryUsage(long memoryMb)
    {
        const long maxMemoryMb = 500; // 500 MB threshold

        if (memoryMb > maxMemoryMb)
        {
            _logger.LogWarning("High memory usage detected: {MemoryMb}MB", memoryMb);
        }
        else
        {
            _logger.LogDebug("Memory usage: {MemoryMb}MB", memoryMb);
        }
    }

    /// <summary>
    /// Checks thread count and logs if excessive.
    /// </summary>
    /// <param name="threadCount">The thread count to check.</param>
    private void CheckThreadCount(int threadCount)
    {
        const int maxThreads = 50; // Threshold for warnings

        if (threadCount > maxThreads)
        {
            _logger.LogWarning("High thread count: {ThreadCount}", threadCount);
        }
        else
        {
            _logger.LogDebug("Thread count: {ThreadCount}", threadCount);
        }
    }

    /// <summary>
    /// Checks network connectivity (placeholder for actual implementation).
    /// </summary>
    private async Task CheckConnectivityAsync()
    {
        try
        {
            // In a real implementation, this would ping the Notion API
            // and log connectivity issues
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connectivity check failed");
        }
    }

    /// <summary>
    /// Gets current health status.
    /// </summary>
    public HealthStatus GetStatus()
    {
        return new HealthStatus
        {
            IsHealthy = _memoryUsageMb < 500 && _threadCount < 50,
            MemoryUsageMb = _memoryUsageMb,
            ThreadCount = _threadCount,
            UptimeSeconds = _uptimeSeconds,
            CheckedAt = _checkedAt
        };
    }

    /// <summary>
    /// Gets whether the application is currently healthy.
    /// </summary>
    public bool IsHealthy => GetStatus().IsHealthy;

    /// <summary>
    /// Gets the current memory usage in megabytes.
    /// </summary>
    public long MemoryUsageMb => _memoryUsageMb;

    /// <summary>
    /// Gets the current thread count.
    /// </summary>
    public int ThreadCount => _threadCount;

    /// <summary>
    /// Gets the current uptime in seconds.
    /// </summary>
    public long UptimeSeconds => _uptimeSeconds;

    /// <summary>
    /// Gets the timestamp when health was last checked.
    /// </summary>
    public DateTime CheckedAt => _checkedAt;

    /// <summary>
    /// Gets whether the worker is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Represents the health status of the application.
/// </summary>
public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public long MemoryUsageMb { get; set; }
    public int ThreadCount { get; set; }
    public long UptimeSeconds { get; set; }
    public DateTime CheckedAt { get; set; }

    public override string ToString()
    {
        var status = IsHealthy ? "✓ Healthy" : "✗ Unhealthy";
        return $@"{status}
Memory: {MemoryUsageMb}MB
Threads: {ThreadCount}
Uptime: {UptimeSeconds}s
Checked: {CheckedAt:g}";
    }
}