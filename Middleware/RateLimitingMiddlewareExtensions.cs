#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="RateLimitingMiddleware"/> providing additional functionality
/// for rate limiting operations and monitoring.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Executes an operation with automatic retry when rate limit is exceeded.
    /// Retries with exponential backoff until successful or timeout occurs.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance</param>
    /// <param name="operation">The operation to execute</param>
    /// <param name="apiService">Name of the API service being called</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds (default: 30)</param>
    /// <returns>The result of the operation</returns>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        this RateLimitingMiddleware middleware,
        Func<Task<T>> operation,
        string apiService = "Unknown",
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        if (maxRetries < 0)
            throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var retryCount = 0;
        var lastException = (Exception?)null;

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                return await middleware.ExecuteWithRateLimitAsync(operation, apiService);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                retryCount++;

                if (retryCount >= maxRetries)
                    break;

                // Exponential backoff: 1s, 2s, 4s, etc.
                var delayMs = Math.Min(1000 * (int)Math.Pow(2, retryCount - 1), 10000);
                await Task.Delay(delayMs, cancellationTokenSource.Token);
            }
        }

        throw new RateLimitExceededException(
            $"Rate limit exceeded after {retryCount} retries. Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Executes an operation with automatic retry when rate limit is exceeded (synchronous version).
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance</param>
    /// <param name="operation">The operation to execute</param>
    /// <param name="apiService">Name of the API service being called</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds (default: 30)</param>
    /// <returns>The result of the operation</returns>
    public static T ExecuteWithRetry<T>(
        this RateLimitingMiddleware middleware,
        Func<T> operation,
        string apiService = "Unknown",
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        if (maxRetries < 0)
            throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var retryCount = 0;
        var lastException = (Exception?)null;

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                return middleware.ExecuteWithRateLimit(operation, apiService);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                retryCount++;

                if (retryCount >= maxRetries)
                    break;

                // Exponential backoff: 1s, 2s, 4s, etc.
                var delayMs = Math.Min(1000 * (int)Math.Pow(2, retryCount - 1), 10000);
                System.Threading.Thread.Sleep(delayMs);
            }
        }

        throw new RateLimitExceededException(
            $"Rate limit exceeded after {retryCount} retries. Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Checks if the rate limit has been exceeded.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance</param>
    /// <returns>True if rate limit has been exceeded; otherwise false</returns>
    public static bool IsRateLimitExceeded(this RateLimitingMiddleware middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        var status = middleware.GetStatus();
        return status.RequestsUsed >= status.LimitPerMinute;
    }

    /// <summary>
    /// Gets the estimated time remaining until the rate limit resets in seconds.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance</param>
    /// <returns>Time remaining in seconds, or 0 if rate limit has not been exceeded</returns>
    public static double GetTimeUntilReset(this RateLimitingMiddleware middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        var status = middleware.GetStatus();
        var now = DateTime.UtcNow;

        if (status.RequestsUsed >= status.LimitPerMinute && status.WindowResetAt > now)
        {
            return (status.WindowResetAt - now).TotalSeconds;
        }

        return 0;
    }

    /// <summary>
    /// Executes an operation only if the rate limit has not been exceeded.
    /// Returns a boolean indicating whether the operation was executed.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance</param>
    /// <param name="operation">The operation to execute</param>
    /// <param name="result">The result of the operation if executed</param>
    /// <param name="apiService">Name of the API service being called</param>
    /// <returns>True if operation was executed; false if rate limit was exceeded</returns>
    public static bool TryExecuteWithRateLimit<T>(
        this RateLimitingMiddleware middleware,
        Func<T> operation,
        out T result,
        string apiService = "Unknown")
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        if (middleware.IsRateLimitExceeded())
        {
            result = default!;
            return false;
        }

        result = middleware.ExecuteWithRateLimit(operation, apiService);
        return true;
    }
}

/// <summary>
/// Exception thrown when rate limit is exceeded after retries.
/// </summary>
public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}