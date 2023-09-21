#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Threading;
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
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="middleware">The rate limiting middleware instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="apiService">Name of the API service being called.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be non-negative.</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds. Must be positive.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="middleware"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="maxRetries"/> is negative or <paramref name="timeoutSeconds"/> is not positive.</exception>
    /// <exception cref="RateLimitExceededException">Thrown when rate limit is exceeded after all retries.</exception>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        this RateLimitingMiddleware middleware,
        Func<Task<T>> operation,
        string apiService = "Unknown",
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentNullException.ThrowIfNull(operation);

        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutSeconds, 0);

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var retryCount = 0;
        Exception? lastException = null;

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                return await middleware.ExecuteWithRateLimitAsync(operation, apiService).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                retryCount++;

                if (retryCount >= maxRetries)
                    break;

                // Exponential backoff: 1s, 2s, 4s, etc. with jitter
                var delayMs = Math.Min(1000 * (int)Math.Pow(2, retryCount - 1), 10000);
                var jitter = Random.Shared.Next(0, 200);
                await Task.Delay(delayMs + jitter, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        throw new RateLimitExceededException(
            $"Rate limit exceeded after {retryCount} retries. Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Executes an operation with automatic retry when rate limit is exceeded (synchronous version).
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="middleware">The rate limiting middleware instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="apiService">Name of the API service being called.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be non-negative.</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds. Must be positive.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="middleware"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="maxRetries"/> is negative or <paramref name="timeoutSeconds"/> is not positive.</exception>
    /// <exception cref="RateLimitExceededException">Thrown when rate limit is exceeded after all retries.</exception>
    public static T ExecuteWithRetry<T>(
        this RateLimitingMiddleware middleware,
        Func<T> operation,
        string apiService = "Unknown",
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentNullException.ThrowIfNull(operation);

        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutSeconds, 0);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var retryCount = 0;
        Exception? lastException = null;

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

                // Exponential backoff: 1s, 2s, 4s, etc. with jitter
                var delayMs = Math.Min(1000 * (int)Math.Pow(2, retryCount - 1), 10000);
                var jitter = Random.Shared.Next(0, 200);
                Thread.Sleep(delayMs + jitter);
            }
        }

        throw new RateLimitExceededException(
            $"Rate limit exceeded after {retryCount} retries. Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Checks if the rate limit has been exceeded.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance.</param>
    /// <returns>True if rate limit has been exceeded; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="middleware"/> is <see langword="null"/>.</exception>
    public static bool IsRateLimitExceeded(this RateLimitingMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        var status = middleware.GetStatus();
        return status.RequestsUsed >= status.LimitPerMinute;
    }

    /// <summary>
    /// Gets the estimated time remaining until the rate limit resets in seconds.
    /// </summary>
    /// <param name="middleware">The rate limiting middleware instance.</param>
    /// <returns>Time remaining in seconds, or 0 if rate limit has not been exceeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="middleware"/> is <see langword="null"/>.</exception>
    public static double GetTimeUntilReset(this RateLimitingMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        var status = middleware.GetStatus();
        var now = DateTime.UtcNow;

        return status.RequestsUsed >= status.LimitPerMinute && status.WindowResetAt > now
            ? (status.WindowResetAt - now).TotalSeconds
            : 0;
    }

    /// <summary>
    /// Executes an operation only if the rate limit has not been exceeded.
    /// Returns a boolean indicating whether the operation was executed.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="middleware">The rate limiting middleware instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="result">The result of the operation if executed.</param>
    /// <param name="apiService">Name of the API service being called.</param>
    /// <returns>True if operation was executed; false if rate limit was exceeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="middleware"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    public static bool TryExecuteWithRateLimit<T>(
        this RateLimitingMiddleware middleware,
        Func<T> operation,
        out T result,
        string apiService = "Unknown")
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentNullException.ThrowIfNull(operation);

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
public sealed class RateLimitExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}