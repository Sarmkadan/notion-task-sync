# SyncService

`SyncService` orchestrates bidirectional synchronization between a local task store and Notion pages. It compares local tasks against their Notion counterparts, detects additions, modifications, and deletions on both sides, resolves conflicts according to configurable strategies, and persists the resulting state. Each execution produces a `SyncResult` that records counts of detected changes, conflict outcomes, and per-operation tallies.

## API

### Constructors

- **`public SyncService`**  
  Initializes a new instance configured for a specific synchronization profile. The constructor expects the configuration identifier and any required dependencies (such as local task repository and Notion client abstractions) to be supplied by the caller or via dependency injection. Construction does not start synchronization; call `ExecuteSyncAsync` to perform a run.

### Methods

- **`public async Task<SyncResult> ExecuteSyncAsync`**  
  Runs a full synchronization cycle.  
  **Returns:** A `SyncResult` containing counts of local/Notion changes, conflict statistics, and per-operation numbers (created, updated, deleted, unchanged).  
  **Throws:** May throw `InvalidOperationException` if the service is already executing a sync cycle, `TaskCanceledException` if cancellation is requested, or provider-specific exceptions when the local store or Notion API is unreachable.

- **`public async Task<List<SyncResult>> GetSyncHistoryAsync`**  
  Retrieves the history of synchronization results for the configured profile, ordered from most recent to oldest.  
  **Returns:** A list of `SyncResult` objects representing past executions. Returns an empty list if no history exists.  
  **Throws:** May throw if the underlying history store is unavailable or corrupted.

### Properties

- **`public Guid ConfigId`**  
  The unique identifier of the synchronization configuration this instance operates under.

- **`public DateTime StartedAt`**  
  The UTC timestamp when the current or most recent synchronization cycle began.

- **`public DateTime? CompletedAt`**  
  The UTC timestamp when the current or most recent synchronization cycle finished, or `null` if no cycle has completed or one is still in progress.

- **`public SyncStatus Status`**  
  The current state of the service. Typical values include `Idle`, `Running`, `Completed`, and `Faulted`.

- **`public int LocalTaskCount`**  
  The total number of local tasks considered during the most recent cycle.

- **`public int NotionPageCount`**  
  The total number of Notion pages considered during the most recent cycle.

- **`public int LocalChangesDetected`**  
  The number of local tasks that differed from their last known synchronized state.

- **`public int NotionChangesDetected`**  
  The number of Notion pages that differed from their last known synchronized state.

- **`public int ConflictsDetected`**  
  The number of items where both the local task and the Notion page had changed since the last synchronization, requiring conflict resolution.

- **`public int ConflictsResolved`**  
  The number of conflicts that were automatically resolved according to the configured resolution strategy.

- **`public int ConflictsPendingReview`**  
  The number of conflicts that could not be resolved automatically and require manual intervention.

- **`public int Created`**  
  The number of items created during the cycle (either locally, in Notion, or both, depending on direction).

- **`public int Updated`**  
  The number of items updated during the cycle.

- **`public int Deleted`**  
  The number of items deleted during the cycle.

- **`public int Unchanged`**  
  The number of items that were examined but required no changes.

- **`public string? ErrorMessage`**  
  A human-readable summary of the error if the cycle faulted; `null` otherwise.

- **`public string? ErrorDetails`**  
  Additional technical details about the error (stack trace, inner exception messages, or provider-specific diagnostics); `null` otherwise.

## Usage

### Example 1: Basic synchronization run

```csharp
var syncService = new SyncService(configId, localTaskRepo, notionClient, historyStore);
SyncResult result = await syncService.ExecuteSyncAsync();

Console.WriteLine($"Sync complete: {result.Created} created, {result.Updated} updated, " +
                  $"{result.Deleted} deleted, {result.Unchanged} unchanged.");
if (result.ConflictsPendingReview > 0)
{
    Console.WriteLine($"Warning: {result.ConflictsPendingReview} conflicts require manual review.");
}
```

### Example 2: Inspecting state and retrieving history

```csharp
var syncService = new SyncService(configId, localTaskRepo, notionClient, historyStore);

// Check status before starting
if (syncService.Status == SyncStatus.Running)
{
    Console.WriteLine("Sync already in progress; skipping.");
    return;
}

SyncResult result = await syncService.ExecuteSyncAsync();

if (syncService.Status == SyncStatus.Faulted)
{
    Console.WriteLine($"Sync failed: {syncService.ErrorMessage}");
    Console.WriteLine($"Details: {syncService.ErrorDetails}");
}

// Retrieve past results
List<SyncResult> history = await syncService.GetSyncHistoryAsync();
foreach (var entry in history.Take(5))
{
    Console.WriteLine($"{entry.StartedAt:u}: {entry.Created} created, {entry.ConflictsDetected} conflicts");
}
```

## Notes

- **Thread safety:** `ExecuteSyncAsync` is not reentrant. Attempting to call it while `Status` is `Running` will throw an `InvalidOperationException`. The properties reflecting cycle statistics (`LocalTaskCount`, `NotionPageCount`, etc.) are populated only after a cycle completes and should be read from a single thread or behind a synchronization mechanism if accessed concurrently with a new cycle start.
- **Property lifetime:** All numeric counters and timestamps are reset at the beginning of each new `ExecuteSyncAsync` call. Reading them before the first execution yields default values (zero counts, `StartedAt` at its default, `CompletedAt` null, `Status` `Idle`).
- **Error state:** When `Status` is `Faulted`, `ErrorMessage` and `ErrorDetails` are populated. Counters may reflect partial progress depending on where the failure occurred; do not assume they represent a complete or consistent state.
- **History independence:** `GetSyncHistoryAsync` operates on persisted records and is safe to call regardless of the current `Status`. It does not reflect an in-progress cycle until that cycle completes and its result is persisted.
- **Conflict resolution:** `ConflictsResolved` and `ConflictsPendingReview` together sum to `ConflictsDetected`. A non-zero `ConflictsPendingReview` indicates that the synchronization left some items intentionally out of sync; downstream processes must handle these.
