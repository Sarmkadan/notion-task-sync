# SyncWorker
The `SyncWorker` class encapsulates a background service responsible for periodically synchronizing data with the Notion API. It provides a simple lifecycle interface to start, stop, and clean up the worker, ensuring that synchronization tasks run on a dedicated thread and are properly disposed when no longer needed.

## API
### SyncWorker
**Purpose**  
Initializes a new instance of the `SyncWorker`. The worker is created in a stopped state and must be started explicitly via `Start`.

**Parameters**  
None.

**Return value**  
A new `SyncWorker` instance.

**Exceptions**  
None.

### Start
**Purpose**  
Begins the synchronization process. After this method returns, the worker runs its internal loop on a background thread until `StopAsync` is called or the instance is disposed.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` – The worker is already started.  
- `ObjectDisposedException` – The worker has been disposed.

### StopAsync
**Purpose**  
Requests a graceful shutdown of the worker. The method returns a `Task` that completes when the background loop has exited and all resources have been released.

**Parameters**  
None.

**Return value**  
A `Task` that represents the asynchronous stop operation.

**Exceptions**  
- `ObjectDisposedException` – The worker has been disposed.

### Dispose
**Purpose**  
Releases all unmanaged resources used by the worker and optionally disposes of managed resources. After disposal, the worker cannot be restarted.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `ObjectDisposedException` – The worker has already been disposed.

## Usage
### Basic start‑stop pattern
```csharp
using var worker = new SyncWorker();
worker.Start();
// Perform other work while synchronization runs in the background.
await worker.StopAsync();
```
The `using` statement ensures `Dispose` is called even if an exception occurs before `StopAsync` is awaited.

### Handling start errors
```csharp
var worker = new SyncWorker();
try
{
    worker.Start();
    // … do work …
}
catch (InvalidOperationException ex)
{
    // Log or handle the case where Start was called twice.
    Console.WriteLine($"Worker already started: {ex.Message}");
}
finally
{
    if (!worker.Equals(null))
    {
        // StopAsync may be called only if Start succeeded.
        if (worker.GetType().GetMethod(nameof(StopAsync)) != null)
        {
            _ = worker.StopAsync(); // fire‑and‑forget for cleanup in sync context
        }
        worker.Dispose();
    }
}
```
This example demonstrates defensive programming when the worker might be reused or when `Start` could fail.

## Notes
- Calling `Start` more than once without an intervening `StopAsync` (or `Dispose`) results in an `InvalidOperationException`.  
- `StopAsync` is safe to call multiple times; subsequent calls return a completed task immediately.  
- `Dispose` is thread‑safe and may be invoked from any thread; after disposal, both `Start` and `StopAsync` throw `ObjectDisposedException`.  
- The worker does not accept a cancellation token; cancellation is coordinated solely via `StopAsync`.  
- Background work is performed on a ThreadPool thread; long‑running synchronization blocks should periodically check for disposal status to avoid delayed shutdown.  
- No guarantees are made about the ordering of exceptions; callers should handle `ObjectDisposedException` as a terminal state.
