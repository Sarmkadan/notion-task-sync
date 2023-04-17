// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware that logs all sync operations including duration, status, and errors.
/// Provides observability into what operations are happening and their performance characteristics.
/// Critical for debugging and monitoring application health in production.
/// </summary>
public class LoggingMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Wraps an async operation with logging of duration and result.
    /// Logs operation start, completion, duration, and any errors encountered.
    /// </summary>
    public async Task<T> ExecuteWithLoggingAsync<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting operation: {OperationName}", operationName);

            var result = await operation();

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed operation: {OperationName} in {Duration}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Failed operation: {OperationName} after {Duration}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Wraps a sync operation with logging.
    /// Similar to async version but for synchronous operations.
    /// </summary>
    public T ExecuteWithLogging<T>(
        string operationName,
        Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting operation: {OperationName}", operationName);

            var result = operation();

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed operation: {OperationName} in {Duration}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Failed operation: {OperationName} after {Duration}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Logs structured information about a sync result.
    /// Used to create consistent, searchable log entries for analysis.
    /// </summary>
    public void LogSyncOperation(
        string operationName,
        string status,
        int itemCount,
        int changeCount,
        TimeSpan duration)
    {
        _logger.LogInformation(
            "Sync operation completed - Name: {OperationName}, Status: {Status}, Items: {ItemCount}, Changes: {ChangeCount}, Duration: {Duration}ms",
            operationName,
            status,
            itemCount,
            changeCount,
            duration.TotalMilliseconds);
    }

    /// <summary>
    /// Logs a warning about a potentially problematic condition.
    /// Used for things like slow operations, high conflict counts, etc.
    /// </summary>
    public void LogWarning(string operationName, string message, params object[] args)
    {
        _logger.LogWarning("Operation {OperationName}: " + message,
            new object[] { operationName }.Concat(args).ToArray());
    }

    /// <summary>
    /// Logs debugging information with operation context.
    /// Helpful for troubleshooting when verbose logging is enabled.
    /// </summary>
    public void LogDebug(string operationName, string message, params object[] args)
    {
        _logger.LogDebug("Operation {OperationName}: " + message,
            new object[] { operationName }.Concat(args).ToArray());
    }
}
