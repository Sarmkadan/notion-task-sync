# RetryHelper

`RetryHelper` provides configurable retry and circuit-breaker patterns for transient-fault handling. It encapsulates retry policies, delays, and circuit-breaking logic so that callers can wrap operations that may fail intermittently without writing repetitive try/catch scaffolding.

## API

### `public RetryHelper`
The constructor. Initializes the helper with the desired retry policy, maximum attempts, base delay, back-off strategy, and optional circuit-breaker thresholds. Specific overloads and parameter names are determined by the implementation; the constructor accepts configuration that governs all subsequent `Execute*` calls made on the instance.

### `public async Task<T> ExecuteWithRetryAsync<T>(…)`
Executes an asynchronous operation with retry logic. When the operation throws a transient exception, the helper waits according to the configured delay strategy and retries up to the maximum attempt count.

- **Parameters:** An async factory delegate `Func<Task<T>>` (or equivalent) that returns `Task<T>`, along with optional cancellation token and per-call overrides.
- **Return value:** `Task<T>` – the result of the first successful invocation.
- **Throws:** The last captured exception if all retries are exhausted. `OperationCanceledException` if the cancellation token is signaled. Immediate rethrow for non-transient exceptions (as defined by the configured exception filter).

### `public async Task<T> ExecuteWithRetryAsync<T>(…)` (second overload)
Same semantics as the first overload but accepts a synchronous factory `Func<T>` and wraps it for async execution. This allows retrying CPU-bound or hybrid work without forcing the caller to supply an async delegate.

- **Parameters:** A synchronous factory `Func<T>`, optional cancellation token, and per-call overrides.
- **Return value:** `Task<T>` – the result of the first successful invocation.
- **Throws:** Same as the async overload.

### `public T ExecuteWithRetry<T>(…)`
Synchronous retry execution. Blocks the calling thread during delays. Suitable for console applications, synchronous services, or contexts where async/await is unavailable.

- **Parameters:** A synchronous factory `Func<T>`, optional cancellation token, and per-call overrides.
- **Return value:** `T` – the result of the first successful invocation.
- **Throws:** The last captured exception when retries are exhausted. `OperationCanceledException` on cancellation. Non-transient exceptions propagate immediately.

### `public async Task<(T? result, bool success)> ExecuteWithCircuitBreakerAsync<T>(…)`
Executes an asynchronous operation under a circuit-breaker guard. If the circuit is open (too many recent failures), the call is short-circuited without invoking the factory. On success the result is returned with `success = true`; on failure or open circuit the method returns `(default(T), false)` rather than throwing.

- **Parameters:** An async factory `Func<Task<T>>`, optional cancellation token, and per-call overrides.
- **Return value:** `Task<(T? result, bool success)>` – a tuple where `success` indicates whether the operation completed without a handled fault and `result` contains the value (or `default` when `success` is `false`).
- **Throws:** `OperationCanceledException` on cancellation. Does **not** throw on exhausted retries or open circuit; those are communicated via `success = false`.

## Usage

### Example 1: Async HTTP call with retry
```csharp
var retry = new RetryHelper(maxAttempts: 3, baseDelay: TimeSpan.FromMilliseconds(200));

string data = await retry.ExecuteWithRetryAsync(
    async ct =>
    {
        using var http = new HttpClient();
        var response = await http.GetAsync("https://api.example.com/items", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    },
    CancellationToken.None);
```

### Example 2: Circuit-breaker for a flaky database connection
```csharp
var breaker = new RetryHelper(
    maxAttempts: 2,
    baseDelay: TimeSpan.FromSeconds(1),
    circuitBreakThreshold: 5,
    circuitResetTimeout: TimeSpan.FromSeconds(30));

var (result, success) = await breaker.ExecuteWithCircuitBreakerAsync(
    async ct =>
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM events";
        return (long?)await cmd.ExecuteScalarAsync(ct);
    });

if (success)
    Console.WriteLine($"Event count: {result}");
else
    Console.WriteLine("Operation skipped (circuit open or retries exhausted).");
```

## Notes

- **Exception filtering:** The helper distinguishes transient faults (e.g., `HttpRequestException`, `SqlException` with certain error codes, timeouts) from non-transient ones (e.g., `NullReferenceException`, `ArgumentException`). Non-transient exceptions bypass retry and propagate immediately. The exact filter is configurable at construction time.
- **Circuit state:** `ExecuteWithCircuitBreakerAsync` tracks failures across calls. Once the failure count reaches the threshold within the tracking window, the circuit opens and subsequent calls return `(default, false)` without invoking the factory. After the reset timeout elapses, the circuit transitions to half-open; the next call is attempted and, if successful, closes the circuit.
- **Thread safety:** All `Execute*` methods are safe to call concurrently on the same instance. Circuit-breaker counters and state transitions use atomic operations or locking internally. Delay timers do not block shared resources.
- **Cancellation:** When a cancellation token is signaled during a delay, the delay is cancelled and `OperationCanceledException` is thrown. If cancellation occurs during factory execution, the exception surfaces according to the factory’s own cancellation behavior.
- **Return value on failure:** `ExecuteWithCircuitBreakerAsync` returns `default(T)` when `success` is `false`. For reference types this is `null`; callers must check `success` before consuming `result`.
- **Synchronous overload:** `ExecuteWithRetry<T>` uses `Thread.Sleep` or equivalent blocking waits. It is not suitable for UI threads or ASP.NET request contexts; prefer the async overloads in those environments.
