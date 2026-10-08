# Middleware

The `Middleware/` folder holds three cross-cutting helpers used around sync and Notion API calls:

| Class | Purpose | Lifetime in code |
|---|---|---|
| `ErrorHandlingMiddleware` | Maps exceptions to messages, status codes and log levels; retry helpers | Constructed by the caller (not registered in DI) |
| `LoggingMiddleware` | Wraps operations with start/complete/fail logs and a duration | Constructed by the caller (not registered in DI) |
| `RateLimitingMiddleware` | Client-side throttle for Notion API requests, with a warning event near the limit | Constructed by the caller (not registered in DI) |

Extension classes next to them (`ErrorHandlingMiddlewareExtensions`, `RateLimitingMiddlewareExtensions`) add retry and status helpers.

## Status in the default path

The middleware is **not wired into the default `Program.Main` path**. `Program.cs` builds DI by hand, and no registration of these classes exists in `Infrastructure/Configuration/DependencyInjection.cs` or `Services/SyncServiceExtensions.cs`. See `docs/ARCHITECTURE.md` ("Extension points"). The behavior below describes the classes as written. Nothing in the running sync applies them today.

## Order

There is no pipeline or ordering mechanism in code. The classes are plain objects that a caller wraps around an operation. If they are composed, the order follows from the call nesting:

1. **RateLimitingMiddleware** (innermost, closest to the HTTP call). Each attempt waits for the throttle before the request is sent.
2. **LoggingMiddleware** (wraps one attempt, timing it).
3. **ErrorHandlingMiddleware** (outermost). It converts the final exception into a result tuple and decides whether to retry.

`RateLimitingMiddlewareExtensions.ExecuteWithRetry` already nests rate limiting inside its own retry loop, so retries also pass through the throttle.

## ErrorHandlingMiddleware

Constructor: `ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)`. The logger is required (`ArgumentNullException` on null).

Main methods:

- `TryExecuteAsync<T>(operationName, operation)` / `TryExecute<T>(...)`: returns `(result, success, error)`. Never throws for the handled types.
- `ExecuteAsync(operationName, operation)`: rethrows after logging `Failed operation`.
- `GetStatusCode(Exception)`: `ConfigurationException` 400, `SyncException` 422, `TimeoutException` 408, `InvalidOperationException` 409, other 500.
- `FormatErrorMessage(Exception)`: user-facing text. Unknown exceptions give a generic message, so internal details are not shown.
- `IsRetryable(Exception)`: `TimeoutException`, `HttpRequestException`, `OperationCanceledException` are retryable. Everything else is not.

Log levels from `TryExecuteAsync`:

| Exception | Level |
|---|---|
| `ConfigurationException` | Error |
| `SyncException` | Error |
| `TimeoutException` | Warning |
| `InvalidOperationException` | Warning |
| Any other | Error |

Extension helpers (`ErrorHandlingMiddlewareExtensions`):

- `SafeExecuteAsync(operationName, Func<Task>)`: returns `(success, error)`.
- `ExecuteWithRetryAsync(operationName, Func<Task<T>>, maxRetries = 3)`: retries while `IsRetryable` is true, with a linear delay of `100 * attempt` ms. `maxRetries` must be non-negative.
- `ExecuteWithStatusAsync` / `ExecuteWithStatus`: returns `(result, statusCode, error)`.

**Known behavior to be aware of:** `ExecuteWithRetryAsync` checks `IsRetryable` against `new Exception(error)`, which is a plain `Exception`. That type is never retryable, so the loop stops after the first failed attempt. The retry path is effectively inactive. This is documented here and is not changed by this doc.

## LoggingMiddleware

Constructor: `LoggingMiddleware(ILogger<LoggingMiddleware> logger)`.

- `ExecuteWithLoggingAsync<T>(operationName, Func<Task<T>>)` and `ExecuteWithLogging<T>(...)`: logs `Starting operation`, then `Completed operation ... in {Duration}ms` (Information). On exception it logs `Failed operation ... after {Duration}ms` (Error) and rethrows.
- `LogSyncOperation(...)`: Information line with a `TimeSpan` duration.
- `LogWarning(operationName, message, args)` and `LogDebug(operationName, message, args)`: prefix the message with `Operation {OperationName}:`.

LoggingMiddleware does not catch exceptions into results. It only logs and rethrows.

## RateLimitingMiddleware

Constructor:

```csharp
new RateLimitingMiddleware(int requestsPerMinute, EventBus eventBus, ILogger<RateLimitingMiddleware> logger)
```

- `requestsPerMinute` must be positive (`ArgumentException` otherwise).
- `eventBus` and `logger` are required.

Throttle behavior (fixed 60-second window, in-memory counters):

- If the window has used all `requestsPerMinute`, the call waits until the window ends, plus 100 ms.
- If the count is ahead of an even pace (`requests per second * elapsed`), the call waits 500 ms.
- Otherwise the call runs immediately.
- The window resets after 60 seconds from its start.
- When the count reaches 80% of the limit, a `RateLimitWarningEvent` is published on the `EventBus` with `ApiService`, `RequestsRemaining`, `RequestLimit` and `ResetTime`.

Methods:

- `ExecuteWithRateLimitAsync<T>(Func<Task<T>>, apiService)` and the synchronous `ExecuteWithRateLimit<T>(Func<T>, apiService)`.
- `ProcessRateLimitHeader(remaining, resetEpoch, apiService)`: when the API reports fewer than 20% of the limit remaining, logs a Warning. The reset time is computed but not stored.
- `GetStatus()` returns `RateLimitStatus` (`RequestsUsed`, `RequestsRemaining`, `LimitPerMinute`, `WindowResetAt`, plus `UsagePercentage`, `IsHigh` at above 80%, `IsCritical` at above 95%).
- `Reset()`: clears the counters.

`RateLimitingMiddlewareExtensions`:

- `ExecuteWithRetryAsync` / `ExecuteWithRetry(operation, apiService, maxRetries, timeoutSeconds)`: retries rate-limited calls with delay and jitter, bounded by a timeout. Throws `RateLimitExceededException` when retries or the timeout run out. `maxRetries` must be non-negative and `timeoutSeconds` positive.
- `TryExecuteWithRateLimit<T>(...)`: returns false without running when the limit is already reached.
- `IsRateLimitExceeded()`, `GetTimeUntilReset()`: read-only status checks.

## Configuration

The Notion settings in `appsettings.json` and `Infrastructure/Configuration/NotionApiSettings.cs` include:

```json
"RateLimitPerMinute": 30,
"RespectRateLimits": true
```

Current state:

- `RateLimitPerMinute` (default 30) is the intended input for `RateLimitingMiddleware`. No code currently passes it to the middleware constructor.
- `RespectRateLimits` is read by no middleware class.
- `ErrorHandlingMiddleware` and `LoggingMiddleware` have no settings. Their behavior is fixed in code.

To use the middleware, construct it with `RateLimitPerMinute` and the required services. Register the instances in DI yourself, following the existing pattern in `Infrastructure/Configuration/DependencyInjection.cs`.
