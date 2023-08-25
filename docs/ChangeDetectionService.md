# ChangeDetectionService

Service responsible for comparing local task state with Notion state, identifying changes, conflicts, and providing change history.

## API

### `public ChangeDetectionService()`
Creates a new instance of the service. The constructor expects the implementing class to have its dependencies (e.g., task repository, Notion client) supplied via dependency injection or property initialization before any method is invoked.  
**Throws:**  
- `InvalidOperationException` if required dependencies are not set before calling any detection method.

### `public List<ChangeLog> DetectLocalChanges()`
Scans the local task store and returns a list of `ChangeLog` entries representing modifications that have occurred since the last successful synchronization.  
**Parameters:** None.  
**Return Value:** A list of `ChangeLog` objects; empty list if no local changes are detected.  
**Throws:**  
- `InvalidOperationException` if the service has not been properly initialized.  
- `IOException` if accessing the local storage fails.

### `public List<ChangeLog> DetectNotionChanges()`
Queries Notion for updates to tasks and returns a list of `ChangeLog` entries representing changes found on the remote side.  
**Parameters:** None.  
**Return Value:** A list of `ChangeLog` objects; empty list if Notion reports no changes.  
**Throws:**  
- `InvalidOperationException` if the service lacks a configured Notion client.  
- `NotionApiException` (or derived) if the request to Notion fails.

### `public List<ConflictResolution> DetectConflicts(List<ChangeLog> localChanges, List<ChangeLog> notionChanges)`
Compares two collections of `ChangeLog` entries—one from the local store and one from Notion—and produces a list of `ConflictResolution` objects describing any conflicting updates.  
**Parameters:**  
- `localChanges`: The list returned by `DetectLocalChanges`. Must not be `null`.  
- `notionChanges`: The list returned by `DetectNotionChanges`. Must not be `null`.  
**Return Value:** A list of `ConflictResolution` instances; empty if no conflicts exist.  
**Throws:**  
- `ArgumentNullException` if either parameter is `null`.  
- `InvalidOperationException` if the service state is inconsistent.

### `public List<ChangeLog> GetTaskChangeHistory(string taskId)`
Retrieves the chronological history of changes for a specific task identified by `taskId`.  
**Parameters:**  
- `taskId`: The unique identifier of the task; must not be `null` or whitespace.  
**Return Value:** A list of `ChangeLog` objects ordered from oldest to newest; empty list if the task has no recorded changes.  
**Throws:**  
- `ArgumentException` if `taskId` is invalid.  
- `InvalidOperationException` if the underlying history store is unavailable.

### `public bool HasChangedSince(DateTime timestamp)`
Determines whether any task (local or Notion) has undergone a change after the supplied `timestamp`.  
**Parameters:**  
- `timestamp`: The point in time to compare against.  
**Return Value:** `true` if at least one change exists after `timestamp`; otherwise `false`.  
**Throws:**  
- `ArgumentOutOfRangeException` if `timestamp` is in the future relative to the system clock.  
- `InvalidOperationException` if the service cannot access change logs.

### `public ChangeLog? GetLastChange()`
Returns the most recent `ChangeLog` entry across both local and Notion sources, or `null` if no changes have been recorded.  
**Parameters:** None.  
**Return Value:** The latest `ChangeLog` or `null`.  
**Throws:**  
- `InvalidOperationException` if the service is unable to read the change log storage.

### `public static bool ArePropertyValuesEqual(object obj1, object obj2, string propertyName)`
Compares the value of a specified property on two objects for equality, handling `null` references gracefully.  
**Parameters:**  
- `obj1`: First object; may be `null`.  
- `obj2`: Second object; may be `null`.  
- `propertyName`: Name of the property to compare; must not be `null` or whitespace.  
**Return Value:** `true` if both objects have the same property value (or both are `null` for that property); otherwise `false`.  
**Throws:**  
- `ArgumentNullException` if `propertyName` is `null`.  
- `ArgumentException` if `propertyName` does not exist on either object's type.  
- `TargetInvocationException` if the property getter throws during retrieval throws an exception.

## Usage

### Example 1: Detecting and resolving conflicts
```csharp
var syncService = new ChangeDetectionService();
// Assume dependencies have been injected elsewhere

var localChanges = syncService.DetectLocalChanges();
var notionChanges = syncService.DetectNotionChanges();

var conflicts = syncService.DetectConflicts(localChanges, notionChanges);

foreach (var conflict in conflicts)
{
    // Apply custom resolution logic, e.g., prefer Notion side
    syncService.ApplyResolution(conflict.ResolutionAction);
}
```

### Example 2: Checking for recent changes and retrieving history
```csharp
var syncService = new ChangeDetectionService();

var lastSync = DateTime.UtcNow.AddHours(-1);
if (syncService.HasChangedSince(lastSync))
{
    var history = syncService.GetTaskChangeHistory("task-123");
    var latest = syncService.GetLastChange();

    Console.WriteLine($"Task has {history.Count} recorded changes.");
    if (latest != null)
    {
        Console.WriteLine($"Latest change: {latest.Description} at {latest.Timestamp}");
    }
}
```

## Notes
- The service is **not thread-safe**; concurrent calls to instance methods from multiple threads may result in inconsistent state or exceptions. External synchronization is required when sharing an instance across threads.  
- The static method `ArePropertyValuesEqual` is thread-safe as it operates only on its parameters.  
- `DetectLocalChanges` and `DetectNotionChanges` may return empty lists rather than `null`; callers should not rely on `null` to indicate “no changes”.  
- `GetLastChange` returns `null` when the change log is empty or unavailable; callers must handle the null case.  
- Exception types listed are based on typical implementations; actual thrown types may vary but will derive from `System.Exception`.  
- Performance of `DetectConflicts` scales linearly with the size of the input lists; large change sets should be paginated or filtered prior to invocation.  
- The service does not automatically persist resolutions; callers must commit any decided changes via their own storage or Notion update mechanisms.
