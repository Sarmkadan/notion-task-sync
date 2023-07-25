# SyncStartedEvent

`SyncStartedEvent` is a data structure within the `notion-task-sync` application used to encapsulate the state and metrics of a synchronization operation between a local task source and a Notion database. This object provides comprehensive details regarding the configuration context, execution timing, processing metrics, and any conflicts or errors encountered during the synchronization lifecycle.

## API

The following public members are available:

*   **`ChangeType`** (`string`): Defines the nature of the change performed on the task (e.g., "Created", "Updated", "Deleted").
*   **`ChangesDetected`** (`int`): The total number of discrepancies identified between local and remote sources during the synchronization.
*   **`ConflictType`** (`string`): Categorizes the conflict encountered if synchronization required manual resolution or specific conflict handling logic.
*   **`ConflictsResolved`** (`int`): The total number of conflicts that were successfully resolved during the synchronization process.
*   **`DatabaseId`** (`string`): The unique identifier of the target Notion database.
*   **`Duration`** (`TimeSpan`): The total elapsed time taken to complete the synchronization operation.
*   **`ErrorMessage`** (`string?`): Provides diagnostic information if the synchronization failed; null if `Success` is true.
*   **`LocalModifiedAt`** (`DateTime`): The timestamp indicating when the local task was last modified.
*   **`LocalValues`** (`Dictionary<string, object>`): A collection of the task's property values as they existed in the local source.
*   **`RemoteModifiedAt`** (`DateTime`): The timestamp indicating when the corresponding remote Notion task was last modified.
*   **`RemoteValues`** (`Dictionary<string, object>`): A collection of the task's property values as they existed in the remote Notion database.
*   **`Source`** (`string`): Indicates the origin or provider of the synchronization event.
*   **`StartTime`** (`DateTime`): The timestamp marking the commencement of the synchronization operation.
*   **`Success`** (`bool`): Indicates whether the synchronization operation completed successfully.
*   **`SyncConfigId`** (`string`): The unique identifier for the specific synchronization configuration applied.
*   **`TaskId`** (`Guid`): The unique identifier for the specific task processed.
*   **`TaskTitle`** (`string`): The title of the task involved in the synchronization event.
*   **`TasksProcessed`** (`int`): The total count of tasks processed during the synchronization cycle.

## Usage

### Logging Synchronization Metrics

```csharp
public void OnSyncCompleted(SyncStartedEvent syncEvent)
{
    if (syncEvent.Success)
    {
        Console.WriteLine($"Sync {syncEvent.SyncConfigId} finished in {syncEvent.Duration.TotalSeconds} seconds.");
        Console.WriteLine($"Processed {syncEvent.TasksProcessed} tasks with {syncEvent.ChangesDetected} changes.");
    }
}
```

### Analyzing Conflict Data

```csharp
public void HandleConflict(SyncStartedEvent syncEvent)
{
    if (!string.IsNullOrEmpty(syncEvent.ConflictType))
    {
        Console.WriteLine($"Conflict detected for task '{syncEvent.TaskTitle}' ({syncEvent.TaskId})");
        Console.WriteLine($"Conflict Type: {syncEvent.ConflictType}");
        // Access dictionary values for comparison logic
        var localVal = syncEvent.LocalValues.GetValueOrDefault("Status");
        var remoteVal = syncEvent.RemoteValues.GetValueOrDefault("Status");
    }
}
```

## Notes

*   **Thread Safety**: `SyncStartedEvent` is a plain data transfer object and does not implement internal thread synchronization. Instances should be considered immutable after instantiation for safe access across multiple threads, or explicitly locked if they are intended to be modified in a multi-threaded environment.
*   **Incomplete Data**: Depending on when an event is raised in the application lifecycle, certain fields—particularly those related to completion metrics like `Duration`, `TasksProcessed`, or `ErrorMessage`—may contain default values (`0` for integers, `null` for strings) if accessed prematurely.
*   **Nullability**: The `ErrorMessage` property is nullable. Consumers must perform null checks before accessing this property, especially when `Success` is `true`.
