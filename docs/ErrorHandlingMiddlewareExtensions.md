# ErrorHandlingMiddlewareExtensions

The `ErrorHandlingMiddlewareExtensions` class provides a set of static extension methods designed to standardize exception handling and operational results within middleware components of the `notion-task-sync` project. By encapsulating logic for safe execution, automatic retries, and consistent status code reporting, these methods ensure that asynchronous and synchronous operations adhere to uniform error handling patterns, thereby reducing boilerplate code and improving system reliability.

## API

### SafeExecuteAsync
`public static async Task<(bool success, string? error)> SafeExecuteAsync(Func<Task> operation)`

Executes the provided asynchronous operation within a try-catch block to ensure that exceptions are captured and returned safely.

*   **Parameters:**
    *   `operation`: The asynchronous operation to execute.
*   **Returns:** A tuple containing a `success` boolean indicating if the operation completed without exceptions, and an optional `error` message string if an exception occurred.
*   **Throws:** Does not throw exceptions; they are caught and returned as error strings.

### ExecuteWithRetryAsync&lt;T&gt;
`public static async Task<(T? result, bool success, string? error)> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int retryCount = 3)`

Executes an asynchronous operation that returns a result of type `T`, applying a retry mechanism if the operation fails.

*   **Parameters:**
    *   `operation`: The asynchronous operation to execute.
    *   `retryCount`: The number of times to retry the operation upon failure (default is 3).
*   **Returns:** A tuple containing the `result` of type `T`, a `success` boolean, and an optional `error` message string.
*   **Throws:** Does not throw exceptions after exhausting all retries; the final error is returned in the tuple.

### ExecuteWithStatusAsync&lt;T&gt;
`public static async Task<(T? result, int statusCode, string? error)> ExecuteWithStatusAsync<T>(Func<Task<T>> operation)`

Executes an asynchronous operation and maps the outcome to a result value and an HTTP-like status code, facilitating standardized responses.

*   **Parameters:**
    *   `operation`: The asynchronous operation to execute.
*   **Returns:** A tuple containing the `result` of type `T`, an integer `statusCode` reflecting the outcome, and an optional `error` message string.
*   **Throws:** Does not throw exceptions; they are caught and reflected in the status code and error message.

### ExecuteWithStatus&lt;T&gt;
`public static (T? result, int statusCode, string? error) ExecuteWithStatus<T>(Func<T> operation)`

Synchronously executes an operation and maps the outcome to a result value and an integer status code.

*   **Parameters:**
    *   `operation`: The synchronous operation to execute.
*   **Returns:** A tuple containing the `result` of type `T`, an integer `statusCode`, and an optional `error` message string.
*   **Throws:** Does not throw exceptions; they are caught and reflected in the status code and error message.

## Usage

### Example 1: Using SafeExecuteAsync
This example demonstrates wrapping a background task cleanup operation to ensure errors do not crash the middleware.

```csharp
public async Task InvokeAsync(HttpContext context, Func<Task> next)
{
    var (success, error) = await ErrorHandlingMiddlewareExtensions.SafeExecuteAsync(async () =>
    {
        await _cleanupService.PerformCleanupAsync();
    });

    if (!success)
    {
        _logger.LogError("Cleanup failed: {Error}", error);
    }
    
    await next();
}
```

### Example 2: Using ExecuteWithStatusAsync
This example demonstrates fetching a resource with standardized error handling and status code mapping.

```csharp
public async Task<IActionResult> GetTaskStatus(string taskId)
{
    var (result, statusCode, error) = await ErrorHandlingMiddlewareExtensions.ExecuteWithStatusAsync(async () =>
    {
        return await _taskService.GetTaskAsync(taskId);
    });

    if (statusCode != 200)
    {
        return BadRequest(new { Message = error });
    }

    return Ok(result);
}
```

## Notes

*   **Exception Handling:** These methods are designed to encapsulate exceptions entirely. Ensure that logged errors are monitored, as they will not propagate up the call stack to the global middleware exception handler.
*   **Thread Safety:** The methods are stateless and rely on the thread safety of the passed delegates (`Func<Task>`, `Func<Task<T>>`, `Func<T>`). Ensure that the operations provided to these methods are thread-safe if they access shared resources.
*   **Status Codes:** The `statusCode` returned by `ExecuteWithStatus` and `ExecuteWithStatusAsync` is intended to be used as an HTTP status code, but it is not automatically mapped to the `HttpContext.Response.StatusCode`. Manual mapping is required within the consuming middleware.
