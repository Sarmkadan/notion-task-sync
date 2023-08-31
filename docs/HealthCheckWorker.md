# HealthCheckWorker

The `HealthCheckWorker` is a background component that periodically collects basic runtime health metrics—such as memory usage, thread count, and process uptime—and exposes them through properties and a status enum. It is intended to be started early in an application’s lifetime and stopped gracefully during shutdown, providing a lightweight way to monitor the health of the host process without external dependencies.

## API

### `public HealthCheckWorker()`
Creates a new instance of the health check worker. The worker is initially stopped; calling `Start` begins the periodic collection of metrics. No parameters are required. Throws no exceptions under normal conditions.

### `public void Start`
Begins the periodic health‑checking loop. After this method returns, the worker updates its internal counters and the `CheckedAt` timestamp at a fixed interval (implementation‑specific).  
- **Parameters:** none.  
- **Return value:** none.  
- **Throws:**  
  - `InvalidOperationException` if `Start` is called while the worker is already running.  
  - `ObjectDisposedException` if the instance has already been disposed.

### `public async Task StopAsync`
Stops the health‑checking loop asynchronously. The method waits for the current iteration to finish before returning.  
- **Parameters:** none.  
- **Return value:** a `Task` that completes when the worker has stopped.  
- **Throws:**  
  - `ObjectDisposedException` if the instance has been disposed.  
  - Any exception propagated from the underlying timer cancellation logic (rare).

### `public HealthStatus GetStatus`
Returns a snapshot of the current health status based on the most recent metric collection.  
- **Parameters:** none.  
- **Return value:** a `HealthStatus` enum value (`Healthy`, `Degraded`, or `Unhealthy`).  
- **Throws:**  
  - `ObjectDisposedException` if called after `Dispose`.

### `public void Dispose`
Releases any resources held by the worker (e.g., timers, event handles). After disposal, the worker cannot be restarted.  
- **Parameters:** none.  
- **Return value:** none.  
- **Throws:** none (calling `Dispose` multiple times is safe).

### `public bool IsHealthy { get; }`
Convenience property that returns `true` when the latest status is `Healthy`; otherwise `false`.  
- **Parameters:** none.  
- **Return value:** `bool`.  
- **Throws:** `ObjectDisposedException` if accessed after disposal.

### `public long MemoryUsageMb { get; }`
Reports the process’s private memory consumption in megabytes, as measured at the last check.  
- **Parameters:** none.  
- **Return value:** memory usage in MB.  
- **Throws:** `ObjectDisposedException` if accessed after disposal.

### `public int ThreadCount { get; }`
Reports the number of managed threads in the process at the last check.  
- **Parameters:** none.  
- **Return value:** thread count.  
- **Throws:** `ObjectDisposedException` if accessed after disposal.

### `public long UptimeSeconds { get; }`
Reports how many seconds the process has been running since the worker was started (or since process start, depending on implementation).  
- **Parameters:** none.  
- **Return value:** uptime in seconds.  
- **Throws:** `ObjectDisposedException` if accessed after disposal.

### `public DateTime CheckedAt { get; }`
Timestamp of the most recent health metric collection.  
- **Parameters:** none.  
- **Return value:** `DateTime` (local time).  
- **Throws:** `ObjectDisposedException` if accessed after disposal.

### `public override string ToString()`
Returns a human‑readable summary of the worker’s current state, including status, memory usage, thread count, uptime, and the last check time.  
- **Parameters:** none.  
- **Return value:** `string`.  
- **Throws:** none.

## Usage

### Example 1: Simple synchronous usage with `using`

```csharp
using var worker = new HealthCheckWorker();
worker.Start();

// Simulate work while checking health periodically
for (int i = 0; i < 10; i++)
{
    Thread.Sleep(1000);
    if (!worker.IsHealthy)
    {
        Console.WriteLine($"Health degraded at {worker.CheckedAt}");
        // Take corrective action...
    }
}

// Stop and dispose automatically via using block
```

### Example 2: Asynchronous start/stop in an ASP.NET Core hosted service

```csharp
public class HealthCheckHostedService : IHostedService
{
    private readonly HealthCheckWorker _worker = new HealthCheckWorker();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _worker.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _worker.StopAsync();
        _worker.Dispose();
    }
}
```

## Notes

- **Thread safety:** The metric properties (`IsHealthy`, `MemoryUsageMb`, `ThreadCount`, `UptimeSeconds`, `CheckedAt`, `GetStatus`) are safe to read concurrently after `Start` has been called. The methods `Start`, `StopAsync`, and `Dispose` are **not** thread‑safe; concurrent calls may result in undefined behavior or exceptions.
- **Multiple starts:** Calling `Start` a second time without an intervening stop throws `InvalidOperationException`. To restart the worker, first call `StopAsync` (or `Dispose`) and then create a new instance or call `Start` again after confirming the worker has stopped.
- **Access after disposal:** Any attempt to read properties or invoke `GetStatus`, `Start`, or `StopAsync` after `Dispose` has been called will throw `ObjectDisposedException`. It is recommended to wrap the worker usage in a `using` statement or ensure `Dispose` is called exactly once.
- **Exception handling:** `StopAsync` may propagate exceptions from the underlying cancellation mechanism; callers should observe the returned `Task` or await it appropriately.
- **Time source:** `CheckedAt` uses the system local clock; adjustments to the system clock while the worker is running may cause non‑monotonic timestamps.
- **Resource usage:** The worker employs a lightweight timer; its overhead is minimal, but in highly constrained environments consider adjusting the internal interval (if configurable) or disabling the worker when not needed.
