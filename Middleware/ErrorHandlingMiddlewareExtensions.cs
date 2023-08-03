#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for ErrorHandlingMiddleware to provide fluent API and common patterns.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Safely executes an operation and returns a boolean indicating success/failure.
    /// Useful for fire-and-forget scenarios where exceptions should not propagate.
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="operation">The async operation to execute</param>
    /// <returns>Tuple with success flag and error message (null if successful)</returns>
    public static async Task<(bool success, string? error)> SafeExecuteAsync(
        this ErrorHandlingMiddleware middleware,
        string operationName,
        Func<Task> operation)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        if (operationName == null)
        {
            throw new ArgumentNullException(nameof(operationName));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        try
        {
            await middleware.ExecuteAsync(operationName, operation);
            return (true, null);
        }
        catch (Exception ex) when (middleware.IsRetryable(ex))
        {
            return (false, middleware.FormatErrorMessage(ex));
        }
        catch (Exception ex)
        {
            return (false, middleware.FormatErrorMessage(ex));
        }
    }

    /// <summary>
    /// Safely executes an operation with retry logic for transient errors.
    /// Attempts the operation up to the specified number of times.
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <returns>Tuple with result, success flag, and error message</returns>
    public static async Task<(T? result, bool success, string? error)> ExecuteWithRetryAsync<T>(
        this ErrorHandlingMiddleware middleware,
        string operationName,
        Func<Task<T>> operation,
        int maxRetries = 3)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        if (operationName == null)
        {
            throw new ArgumentNullException(nameof(operationName));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");
        }

        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            attempt++;
            var (result, success, error) = await middleware.TryExecuteAsync($"{operationName} (attempt {attempt})", operation);

            if (success)
            {
                return (result, true, null);
            }

            lastException = new Exception(error);

            if (!middleware.IsRetryable(lastException))
            {
                break;
            }

            // Wait before retry - exponential backoff would be better in real implementation
            if (attempt <= maxRetries)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
        }

        return (default, false, middleware.FormatErrorMessage(lastException ?? new Exception("Unknown error")));
    }

    /// <summary>
    /// Executes an operation with automatic status code mapping based on exception type.
    /// Returns a tuple with result, status code, and error message.
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="operation">The async operation to execute</param>
    /// <returns>Tuple with result, status code, and error message</returns>
    public static async Task<(T? result, int statusCode, string? error)> ExecuteWithStatusAsync<T>(
        this ErrorHandlingMiddleware middleware,
        string operationName,
        Func<Task<T>> operation)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        if (operationName == null)
        {
            throw new ArgumentNullException(nameof(operationName));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var (result, success, error) = await middleware.TryExecuteAsync(operationName, operation);

        var statusCode = success ? 200 : middleware.GetStatusCode(new Exception(error));

        return (result, statusCode, error);
    }

    /// <summary>
    /// Executes an operation with automatic status code mapping based on exception type.
    /// Synchronous version that returns appropriate status code and error message.
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="operation">The sync operation to execute</param>
    /// <returns>Tuple with result, status code, and error message</returns>
    public static (T? result, int statusCode, string? error) ExecuteWithStatus<T>(
        this ErrorHandlingMiddleware middleware,
        string operationName,
        Func<T> operation)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        if (operationName == null)
        {
            throw new ArgumentNullException(nameof(operationName));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var (result, success, error) = middleware.TryExecute(operationName, operation);

        var statusCode = success ? 200 : middleware.GetStatusCode(new Exception(error ?? "Unknown error"));

        return (result, statusCode, error);
    }
}