# SyncStatistics
The `SyncStatistics` type is designed to track and provide insights into the synchronization process of tasks, offering a comprehensive overview of the sync operations, including the number of successful and failed syncs, tasks processed, conflicts detected and resolved, and more. This information is crucial for monitoring the efficiency and reliability of the task synchronization mechanism, allowing for the identification of potential issues and areas for improvement.

## API
### Properties
- `TotalSyncs`: The total number of sync operations performed.
- `SuccessfulSyncs`: The number of sync operations that were successful.
- `FailedSyncs`: The number of sync operations that failed.
- `TotalTasksSynced`: The total number of tasks that have been synced.
- `TotalConflicts`: The total number of conflicts detected during sync operations.
- `ResolvedConflicts`: The number of conflicts that have been resolved.
- `Operations`: A list of `SyncOperationSnapshot` objects, each representing a snapshot of a sync operation.
- `LastResetAt`: The date and time when the statistics were last reset.
- `Timestamp`: The date and time of the current sync statistics.
- `DurationMs`: The duration of the sync operation in milliseconds.
- `Successful`: Indicates whether the sync operation was successful.
- `TasksProcessed`: The number of tasks processed during the sync operation.
- `ChangesDetected`: The number of changes detected during the sync operation.
- `ConflictsDetected`: The number of conflicts detected during the sync operation.
- `ConflictsResolved`: The number of conflicts resolved during the sync operation.
- `ErrorMessage`: An error message if the sync operation failed, otherwise null.

### Methods
- `RecordOperation`: Records a sync operation.
- `Reset`: Resets the sync statistics.
- `ToString`: Returns a string representation of the sync statistics.
- `FromSyncResult`: A static method that creates a `SyncOperationSnapshot` from a sync result.

## Usage
```csharp
// Example 1: Basic usage of SyncStatistics
var syncStats = new SyncStatistics();
syncStats.RecordOperation();
Console.WriteLine(syncStats.ToString());

// Example 2: Using SyncStatistics to track sync operations
var syncStats = new SyncStatistics();
try
{
    // Perform sync operation
    syncStats.RecordOperation();
    Console.WriteLine($"Tasks Processed: {syncStats.TasksProcessed}, Changes Detected: {syncStats.ChangesDetected}");
}
catch (Exception ex)
{
    syncStats.ErrorMessage = ex.Message;
    Console.WriteLine($"Sync failed: {syncStats.ErrorMessage}");
}
```

## Notes
- The `SyncStatistics` class is not thread-safe. Access to its members should be synchronized when used in a multi-threaded environment.
- The `Reset` method resets all statistics to their initial state. It should be used with caution to avoid losing valuable information about past sync operations.
- The `FromSyncResult` method provides a convenient way to create a `SyncOperationSnapshot` from a sync result, which can then be recorded using the `RecordOperation` method.
- The `ErrorMessage` property is only set when a sync operation fails. In all other cases, it remains null.
- The `LastResetAt` property can be used to determine when the statistics were last reset, which can be useful for tracking the history of sync operations.
