// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Threading.Tasks;
using NotionTaskSync.Domain.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware that handles exceptions and provides consistent error responses.
/// Converts different exception types into appropriate error messages and log levels.
/// Prevents unhandled exceptions from crashing the application while providing useful feedback.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Wraps an operation to handle exceptions gracefully.
    /// Returns error result instead of throwing, allowing caller to decide next steps.
    /// </summary>
    public async Task<(T? result, bool success, string? error)> TryExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return (result, true, null);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, $"Configuration error: {ex.Message}");
        }
        catch (SyncException ex)
        {
            _logger.LogError(ex, "Sync error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout in {OperationName}", operationName);
            return (default, false, "Operation timed out");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Synchronous version of TryExecuteAsync.
    /// </summary>
    public (T? result, bool success, string? error) TryExecute<T>(
        string operationName,
        Func<T> operation)
    {
        try
        {
            var result = operation();
            return (result, true, null);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError("Configuration error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, $"Configuration error: {ex.Message}");
        }
        catch (SyncException ex)
        {
            _logger.LogError("Sync error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {OperationName}: {Error}", operationName, ex.Message);
            return (default, false, "An error occurred");
        }
    }

    /// <summary>
    /// Determines the appropriate HTTP status code for an exception.
    /// Helps map domain exceptions to standard HTTP responses.
    /// </summary>
    public int GetStatusCode(Exception ex)
    {
        return ex switch
        {
            ConfigurationException => 400, // Bad Request
            SyncException => 422, // Unprocessable Entity
            TimeoutException => 408, // Request Timeout
            InvalidOperationException => 409, // Conflict
            _ => 500 // Internal Server Error
        };
    }

    /// <summary>
    /// Formats an exception into a user-friendly error message.
    /// Hides implementation details while providing useful information.
    /// </summary>
    public string FormatErrorMessage(Exception ex)
    {
        return ex switch
        {
            ConfigurationException => "The application is not properly configured. Please check your settings.",
            SyncException => ex.Message,
            TimeoutException => "The operation took too long to complete. Please try again.",
            InvalidOperationException => ex.Message,
            _ => "An unexpected error occurred. Please try again later."
        };
    }

    /// <summary>
    /// Checks if an exception is retryable (transient error).
    /// Used to determine if an operation should be automatically retried.
    /// </summary>
    public bool IsRetryable(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            HttpRequestException => true, // Network issues
            OperationCanceledException => true,
            _ => false
        };
    }

    /// <summary>
    /// Wraps operation and throws on error, with custom error message.
    /// Simplifies error handling when immediate exception is desired.
    /// </summary>
    public async Task ExecuteAsync(
        string operationName,
        Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed operation: {OperationName}", operationName);
            throw;
        }
    }
}
