# Background Workers

The `Workers/` folder contains two background services: `SyncWorker` (periodic Notion sync) and `HealthCheckWorker` (periodic process health metrics). Both use the same pattern: a dedicated `Task.Run` loop, a `CancellationTokenSource` for shutdown, and an `IsRunning` flag.

Neither worker is registered in `Program.Main` or in `Infrastructure/Configuration/DependencyInjection`. They are library components that the host must construct, start and stop itself (see `docs/ARCHITECTURE.md`, "Extension points").

## Common lifecycle

| Phase | What happens |
|---|---|
| Construction | Dependencies are validated (`ArgumentNullException` on null). The interval is clamped to a minimum of 60 seconds. No thread is started. |
| `Start()` | Creates a new `CancellationTokenSource`, sets `IsRunning = true`, and starts the loop with `Task.Run`. Returns immediately. If already running, logs a warning and returns. |
| Loop | Runs its work, then waits `interval` seconds with `Task.Delay(..., token)`. Cancellation during the delay ends the loop. |
| `StopAsync()` | Cancels the token, sets `IsRunning = false`, and awaits the loop task. If not running, returns (`SyncWorker` logs a warning first). |
| `Dispose()` | Disposes the `CancellationTokenSource`. Does not stop the loop (see Known issues). |

Both workers can be started again after `StopAsync()`, because `Start()` creates a new token source. Neither throws `ObjectDisposedException` or `InvalidOperationException` on double start or stop. A second `Start()` logs a warning and returns.

## SyncWorker

Periodically runs one sync cycle through `SyncService.ExecuteSyncAsync(config)` and publishes events on `EventBus`.

### Constructor

```csharp
public SyncWorker(
    SyncService syncService,
    EventBus eventBus,
    ILogger<SyncWorker> logger,
    SyncConfig config,
    int syncIntervalSeconds = 300)
```

| Parameter | Required | Description |
|---|---|---|
| `syncService` | yes | Runs the sync. Null throws `ArgumentNullException`. |
| `eventBus` | yes | Receives `SyncStartedEvent` and `SyncCompletedEvent`. |
| `logger` | yes | `ILogger<SyncWorker>`. |
| `config` | yes | `SyncConfig` for the sync (`Id`, `NotionDatabaseId` are used in events). |
| `syncIntervalSeconds` | no | Default `300`. Values below `60` are raised to `60`. |

### Loop behavior

Each iteration:

1. Publishes `SyncStartedEvent` (`SyncConfigId`, `DatabaseId`, `Source = "SyncWorker"`).
2. Calls `ExecuteSyncAsync(config)` and measures duration with a `Stopwatch`.
3. Publishes `SyncCompletedEvent` with `TasksProcessed` (local task count), `ChangesDetected` (local + Notion), `ConflictsResolved`, `Duration`, `Success` (`Status == Completed`) and `ErrorMessage`.
4. Updates the failure counter:
   - `Completed`: counter resets to 0.
   - Any other status: counter increases and a warning is logged.
5. If an exception is thrown in steps 1-3, the counter increases and an error is logged.
6. When the counter reaches `3` (`maxConsecutiveFailures`, a constant), the worker calls `StopAsync()` and the loop returns.
7. Otherwise waits `syncIntervalSeconds` before the next iteration.

`ExecuteSyncAsync` does not throw for sync errors. Failures come back as `SyncResult.Status = Failed`, which the worker counts the same way as an exception.

### Failure handling

| Condition | Result |
|---|---|
| 1 or 2 consecutive failures | Logged, loop continues after the normal interval. No backoff; the interval stays fixed. |
| 3 consecutive failures | Error logged, worker stops itself. It does not restart until the host calls `Start()` again. |
| Success | Counter reset. |

### Properties and methods

| Member | Description |
|---|---|
| `Start()` | See common lifecycle. |
| `StopAsync()` | Graceful stop. Waits for the current sync to finish. |
| `IsRunning` | `true` between `Start()` and `StopAsync()`. Set to `false` when the worker stops itself after repeated failures. |
| `Dispose()` | Disposes the token source. Call `StopAsync()` first. |

### Example

```csharp
using var worker = new SyncWorker(syncService, eventBus, logger, config, syncIntervalSeconds: 600);
worker.Start();
// ... host runs ...
await worker.StopAsync();
```

### Configuration options

| Option | Where | Default | Effect |
|---|---|---|---|
| `syncIntervalSeconds` | Constructor argument | `300` | Delay between sync cycles. Minimum `60`. |
| `SyncConfig` | Constructor argument | (caller-supplied) | Which Notion database and local settings the sync uses. |
| Failure threshold | Hard-coded `maxConsecutiveFailures` | `3` | Not configurable. |

The worker has no configuration section in `appsettings.json`. The interval is only set through the constructor.

## HealthCheckWorker

Periodically samples the current process and stores the results. It does not call the Notion API. Its connectivity check is a placeholder.

### Constructor

```csharp
public HealthCheckWorker(ILogger<HealthCheckWorker> logger, int checkIntervalSeconds = 300)
```

| Parameter | Required | Description |
|---|---|---|
| `logger` | yes | `ILogger<HealthCheckWorker>`. Null throws `ArgumentNullException`. |
| `checkIntervalSeconds` | no | Default `300`. Values below `60` are raised to `60`. |

### Loop behavior

Each iteration:

1. Reads from `Process.GetCurrentProcess()`:
   - `WorkingSet64` as `MemoryUsageMb`.
   - `Threads.Count` as `ThreadCount`.
   - `DateTime.UtcNow - StartTime` as `UptimeSeconds`.
2. Stamps `CheckedAt = DateTime.UtcNow`.
3. Logs memory: `Warning` above 500 MB, otherwise `Debug`.
4. Logs threads: `Warning` above 50, otherwise `Debug`.
5. Runs `CheckConnectivityAsync()`. This is a no-op that completes immediately.
6. Waits `checkIntervalSeconds`.

Exceptions inside a check are logged as errors and the loop continues.

### Thresholds

These values are hard-coded in `HealthCheckWorker` and repeated in `HealthCheckWorkerValidation`:

| Metric | Threshold | Used by |
|---|---|---|
| Memory | `> 500 MB` | Warning log, `IsHealthy` |
| Thread count | `> 50` | Warning log, `IsHealthy` |

`IsHealthy` is `true` when memory is below 500 MB and threads are below 50.

### Public members

| Member | Description |
|---|---|
| `Start()` | See common lifecycle. |
| `StopAsync()` | Graceful stop. |
| `GetStatus()` | Returns a `HealthStatus` snapshot: `IsHealthy`, `MemoryUsageMb`, `ThreadCount`, `UptimeSeconds`, `CheckedAt`. |
| `IsHealthy` | Shortcut for `GetStatus().IsHealthy`. |
| `MemoryUsageMb`, `ThreadCount`, `UptimeSeconds`, `CheckedAt` | Values from the last check. All are `0` or `default` before the first check completes. |
| `IsRunning` | `true` between `Start()` and `StopAsync()`. |
| `Dispose()` | Disposes the token source. |

Before the first check, `GetStatus()` reports `IsHealthy = true` (memory 0, threads 0) with `CheckedAt = default`. `HealthCheckWorkerValidation.Validate()` flags a default `CheckedAt` as a problem, so use it only after at least one check has run.

`HealthStatus` is a separate class in the same file. It overrides `ToString()` to print a multi-line summary.

### Validation

`HealthCheckWorkerValidation.Validate(this HealthCheckWorker)` and `Validate(this HealthStatus)` return a list of problems, not exceptions. They check:

- Memory and thread count are non-negative and below the sanity limits (10 GB and 1000 threads).
- Uptime is non-negative and below 10 years.
- `CheckedAt` is set, is not more than 5 minutes in the future, and is not older than 7 days.
- For `HealthStatus`: `IsHealthy` is consistent with the thresholds above.

### Example

```csharp
using var health = new HealthCheckWorker(logger, checkIntervalSeconds: 120);
health.Start();

var status = health.GetStatus();
if (!status.IsHealthy)
{
    logger.LogWarning("Process unhealthy: {Status}", status);
}

await health.StopAsync();
```

### Configuration options

| Option | Where | Default | Effect |
|---|---|---|---|
| `checkIntervalSeconds` | Constructor argument | `300` | Delay between checks. Minimum `60`. |
| Memory threshold | Hard-coded | `500` MB | Warning log and `IsHealthy`. |
| Thread threshold | Hard-coded | `50` | Warning log and `IsHealthy`. |

There is no configuration section for this worker either.

## Known issues

These describe the current code. They are documented here, not fixed.

- **`SyncWorker` self-stop can hang.** After the third consecutive failure, `RunWorkerAsync` calls `StopAsync()`. `StopAsync()` awaits `_workerTask`, which is the task running `RunWorkerAsync`. That task waits on itself, so `StopAsync()` does not return, and the loop does not finish. `IsRunning` is set to `false` in that path, so the host sees the worker as stopped while its task is still pending.
- **`Dispose()` does not stop the loop.** Calling `Dispose()` on a running worker without `StopAsync()` disposes the token source while the loop still uses its token. The next `Task.Delay` may throw `ObjectDisposedException`, which the loop does not catch, and the task faults. Always call `StopAsync()` before `Dispose()`.
- **`HealthCheckWorker` connectivity check is a placeholder.** `CheckConnectivityAsync()` does nothing. `IsHealthy` is based only on memory and threads.
- **Thresholds are duplicated.** The 500 MB and 50 thread values appear in `HealthCheckWorker.cs` (twice, including `GetStatus`) and in `HealthCheckWorkerValidation.cs`. Changing one requires changing the others.

## Related

- `docs/SyncService.md`, `docs/EventBus.md`: dependencies of `SyncWorker`.
- `docs/SyncConfig.md`: the configuration object passed to `SyncWorker`.
- `docs/HealthCheckWorker.md`, `docs/HealthCheckWorkerValidation.md`: per-class API notes. These predate this page and in places do not match the code (for example, they describe a `HealthStatus` enum and a parameterless constructor). This page follows the source.
