# RateLimitingMiddlewareExtensions

The `RateLimitingMiddlewareExtensions` class provides static utility methods designed to enforce and manage rate-limiting policies for outbound API operations within the `notion-task-sync` application. These extensions offer mechanisms to execute operations with automated retry logic, inspect the current rate limit status, and ensure safe API interaction, minimizing the risk of exhaustion of allowed throughput.

## API

### `ExecuteWithRetryAsync<T>`
Executes an asynchronous operation, automatically retrying the call if a rate limit response is received, with delay periods calculated based on service feedback.
*   **Returns:** `Task<T>`
*   **Throws:** `RateLimitExceededException` if retries are exhausted or a non-recoverable error occurs.

### `ExecuteWithRetry<T>`
Executes a synchronous operation, wrapping it in retry logic that handles rate-limited responses by blocking the calling thread for the required wait period.
*   **Returns:** `T`
*   **Throws:** `RateLimitExceededException` if retries are exhausted.

### `IsRateLimitExceeded`
Indicates whether the current application state or context is actively constrained by an active rate limit window.
*   **Returns:** `bool`

### `GetTimeUntilReset`
Calculates and returns the remaining duration, in seconds, before the current rate limit window is expected to reset.
*   **Returns:** `double`

### `TryExecuteWithRateLimit<T>`
Attempts to execute an operation safely, validating against current rate limit constraints before invocation to prevent throttling.
*   **Returns:** `bool` (True if the operation was executed, false if the rate limit precluded execution).

### `RateLimitExceededException`
A specialized exception thrown when an operation cannot be completed because API throughput limits have been exceeded and cannot be recovered via retry mechanisms.

## Usage

### Async Retry Pattern
```csharp
var result = await RateLimitingMiddlewareExtensions.ExecuteWithRetryAsync(async () =>
{
    return await notionClient.Pages.RetrieveAsync(pageId);
});
```

### Conditional Execution
```csharp
if (!RateLimitingMiddlewareExtensions.IsRateLimitExceeded)
{
    var success = RateLimitingMiddlewareExtensions.TryExecuteWithRateLimit(() => PerformDataUpdate());
    if (!success)
    {
        // Handle failed execution attempt
    }
}
else
{
    var delay = RateLimitingMiddlewareExtensions.GetTimeUntilReset();
    Console.WriteLine($"Throttled. Resuming in {delay} seconds.");
}
```

## Notes

*   **Thread Safety:** The static members of `RateLimitingMiddlewareExtensions` manage shared state to track rate limit windows. While these methods are designed to be thread-safe, high-concurrency environments may experience slight latency in state updates.
*   **Error Handling:** `RateLimitExceededException` should be explicitly caught when utilizing methods that do not implement the `Try*` pattern to ensure that throttling errors are handled appropriately without terminating the application process.
*   **Blocking Behavior:** `ExecuteWithRetry` performs synchronous blocking. Use this method with caution to avoid thread starvation within the application, particularly in request-handling pipelines.
