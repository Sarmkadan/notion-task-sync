# RateLimitingMiddleware

`RateLimitingMiddleware` enforces a per-instance rate limit on API calls, tracking request counts within a rolling one-minute window. It provides both synchronous and asynchronous execution wrappers that automatically delay or reject calls when the limit is exceeded, along with methods to inspect current usage and parse rate-limit headers from external responses.

## API

### public RateLimitingMiddleware

Constructor. Initializes a new instance with the specified maximum number of requests allowed per minute.

- **Parameters:**
  - `int limitPerMinute` — The maximum number of requests permitted within a one-minute sliding window.
- **Throws:**
  - `ArgumentOutOfRangeException` — if `limitPerMinute` is less than or equal to zero.

### public async Task\<T\> ExecuteWithRateLimitAsync\<T\>

Asynchronously executes a caller-supplied function, waiting if necessary until a request slot becomes available under the rate limit. The delay is based on the time remaining until the current window resets.

- **Type Parameters:**
  - `T` — The return type of the function.
- **Parameters:**
  - `Func<Task<T>> action` — The asynchronous function to execute.
- **Returns:** The result produced by `action`.
- **Throws:** Exceptions thrown by `action` propagate to the caller. `InvalidOperationException` may be thrown if the middleware state is corrupted.

### public T ExecuteWithRateLimit\<T\>

Synchronously executes a caller-supplied function, blocking the calling thread until a request slot becomes available under the rate limit.

- **Type Parameters:**
  - `T` — The return type of the function.
- **Parameters:**
  - `Func<T> action` — The synchronous function to execute.
- **Returns:** The result produced by `action`.
- **Throws:** Exceptions thrown by `action` propagate to the caller. `InvalidOperationException` may be thrown if the middleware state is corrupted.

### public void ProcessRateLimitHeader

Parses a standard rate-limit response header string and updates the internal window state to match the server’s reported usage. This is typically used to synchronize local tracking with server-side limits after receiving an HTTP response.

- **Parameters:**
  - `string headerValue` — The raw value of a rate-limit header (e.g., `"requests-used=5, requests-remaining=55, window-reset=2025-01-01T12:01:00Z"`).
- **Throws:**
  - `ArgumentNullException` — if `headerValue` is null.
  - `FormatException` — if the header value cannot be parsed into the expected fields.

### public RateLimitStatus GetStatus

Returns a snapshot of the current rate-limit state without modifying it.

- **Returns:** A `RateLimitStatus` object containing `RequestsUsed`, `RequestsRemaining`, `LimitPerMinute`, and `WindowResetAt`.
- **Throws:** None.

### public void Reset

Resets the internal counters and window start time to their initial state, as if no requests have been made. The limit per minute is unchanged.

- **Throws:** None.

### public int RequestsUsed

Gets the number of requests already counted against the limit in the current window.

### public int RequestsRemaining

Gets the number of requests still available before the limit is reached in the current window.

### public int LimitPerMinute

Gets the configured maximum number of requests allowed per one-minute window.

### public DateTime WindowResetAt

Gets the UTC timestamp when the current rate-limit window ends and counters will reset.

## Usage

### Example 1: Asynchronous API calls with automatic throttling

```csharp
var rateLimiter = new RateLimitingMiddleware(limitPerMinute: 30);

async Task<string> FetchDataAsync(int id)
{
    return await rateLimiter.ExecuteWithRateLimitAsync(async () =>
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync($"https://api.example.com/items/{id}");
        return response;
    });
}

// Call in a loop — the middleware will pause when the limit is hit
for (int i = 0; i < 50; i++)
{
    string data = await FetchDataAsync(i);
    Console.WriteLine($"Fetched item {i}: {data.Substring(0, 50)}...");
}
```

### Example 2: Synchronous execution with header synchronization

```csharp
var rateLimiter = new RateLimitingMiddleware(limitPerMinute: 60);

string MakeRequest(string url)
{
    return rateLimiter.ExecuteWithRateLimit(() =>
    {
        using var client = new HttpClient();
        var response = client.GetAsync(url).Result;
        
        // Sync local state with server's reported usage
        if (response.Headers.TryGetValues("X-RateLimit-Status", out var values))
        {
            rateLimiter.ProcessRateLimitHeader(string.Join(",", values));
        }
        
        return response.Content.ReadAsStringAsync().Result;
    });
}

var result = MakeRequest("https://api.example.com/bulk-data");
var status = rateLimiter.GetStatus();
Console.WriteLine($"Remaining: {status.RequestsRemaining}, Window resets: {status.WindowResetAt}");
```

## Notes

- **Thread safety:** The middleware is designed for use from multiple threads or concurrent tasks. Internal state updates during `ExecuteWithRateLimit` and `ExecuteWithRateLimitAsync` are synchronized. However, `ProcessRateLimitHeader`, `Reset`, and reading properties like `RequestsRemaining` are not atomic with respect to in-flight executions; external locking is required if consistent snapshots across multiple members are needed.
- **Window semantics:** The rate-limit window is a fixed one-minute interval starting from the first request after construction or after a `Reset`. When the window expires, counters automatically reset on the next request attempt. `ProcessRateLimitHeader` can override the window reset time based on server-provided values, which may desynchronize the local window from the original one-minute cadence.
- **Blocking behavior:** `ExecuteWithRateLimit` blocks the calling thread using `Thread.Sleep`-style delays. In UI or high-concurrency server contexts, prefer `ExecuteWithRateLimitAsync` to avoid thread-pool starvation.
- **Header parsing:** `ProcessRateLimitHeader` expects a specific format. Malformed headers cause a `FormatException`; the caller should guard against this when consuming headers from sources that may vary in format.
- **Reset impact:** Calling `Reset` discards all current window state. Any waiting callers in `ExecuteWithRateLimitAsync` or `ExecuteWithRateLimit` will proceed immediately after the reset, which may cause a burst exceeding the intended limit if not coordinated externally.
