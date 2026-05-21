#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides retry logic with exponential backoff for handling transient failures.
/// Essential for API calls that may temporarily fail due to rate limits or network issues.
/// Implements industry-standard retry patterns to improve reliability without requiring caller code duplication.
/// </summary>
public sealed class RetryHelper
{
    private readonly ILogger<RetryHelper> _logger;

    public RetryHelper(ILogger<RetryHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with automatic retry on failure using exponential backoff.
    /// Useful for operations that may have transient failures.
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int initialDelayMs = 1000)
    {
        if (maxRetries < 1)
            throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));

        int attempt = 0;
        int delayMs = initialDelayMs;

        while (true)
        {
            attempt++;

            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "Operation failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms. Error: {Error}",
                    attempt,
                    maxRetries,
                    delayMs,
                    ex.Message);

                await Task.Delay(delayMs).ConfigureAwait(false);

                // Exponential backoff: double the delay for next retry, with jitter
                var jitter = new Random().Next(0, delayMs / 2);
                delayMs = (delayMs * 2) + jitter;
            }
            catch (Exception ex) when (attempt >= maxRetries)
            {
                _logger.LogError(
                    "Operation failed after {MaxAttempts} attempts. Giving up. Error: {Error}",
                    maxRetries,
                    ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Executes an operation with automatic retry, allowing caller to determine if retry should occur.
    /// Provides more control over which exceptions trigger retry behavior.
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool> shouldRetry,
        int maxRetries = 3,
        int initialDelayMs = 1000)
    {
        if (maxRetries < 1)
            throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));

        int attempt = 0;
        int delayMs = initialDelayMs;

        while (true)
        {
            attempt++;

            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (shouldRetry(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(
                    "Retryable error on attempt {Attempt}/{MaxAttempts}: {Error}",
                    attempt,
                    maxRetries,
                    ex.Message);

                await Task.Delay(delayMs).ConfigureAwait(false);
                delayMs = (delayMs * 2) + new Random().Next(0, delayMs / 2);
            }
            catch (Exception ex) when (!shouldRetry(ex))
            {
                _logger.LogError("Non-retryable error: {Error}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Operation failed after {MaxAttempts} attempts: {Error}", maxRetries, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Executes a synchronous operation with retry logic.
    /// Blocks the calling thread during retry delays.
    /// </summary>
    public T ExecuteWithRetry<T>(
        Func<T> operation,
        int maxRetries = 3,
        int initialDelayMs = 1000)
    {
        if (maxRetries < 1)
            throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));

        int attempt = 0;
        int delayMs = initialDelayMs;

        while (true)
        {
            attempt++;

            try
            {
                return operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "Operation failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms.",
                    attempt,
                    maxRetries,
                    delayMs);

                System.Threading.Thread.Sleep(delayMs);
                delayMs = (delayMs * 2) + new Random().Next(0, delayMs / 2);
            }
            catch (Exception ex) when (attempt >= maxRetries)
            {
                _logger.LogError("Operation failed after {MaxAttempts} attempts.", maxRetries);
                throw;
            }
        }
    }

    /// <summary>
    /// Implements a circuit breaker pattern - stops retrying after too many failures.
    /// Returns default value instead of throwing after circuit opens.
    /// </summary>
    public async Task<(T? result, bool success)> ExecuteWithCircuitBreakerAsync<T>(
        Func<Task<T>> operation,
        int maxFailures = 5,
        int resetTimeoutMs = 30000)
    {
        var failureCount = 0;
        var lastFailureTime = DateTime.MinValue;

        // Check if circuit should be reset
        if ((DateTime.UtcNow - lastFailureTime).TotalMilliseconds > resetTimeoutMs)
            failureCount = 0;

        if (failureCount >= maxFailures)
        {
            _logger.LogWarning("Circuit breaker is open - too many failures");
            return (default, false);
        }

        try
        {
            var result = await operation().ConfigureAwait(false);
            failureCount = 0; // Reset on success
            return (result, true);
        }
        catch (Exception ex)
        {
            failureCount++;
            lastFailureTime = DateTime.UtcNow;
            _logger.LogError("Operation failed ({FailureCount}/{MaxFailures}): {Error}",
                failureCount,
                maxFailures,
                ex.Message);

            return (default, false);
        }
    }
}
