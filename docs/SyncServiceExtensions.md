# SyncServiceExtensions

SyncServiceExtensions provides a collection of extension methods for the `IEnumerable<SyncService.SyncResult>` type, designed to simplify the analysis and reporting of synchronization operations. These utilities allow for concise evaluation of sync outcomes, including status checks, duration calculation, conflict detection, and result filtering, facilitating streamlined error handling and progress tracking within the `notion-task-sync` application.

## API

The following extension methods are available for instances of `IEnumerable<SyncService.SyncResult>`.

### `IsSuccessful`
```csharp
public static bool IsSuccessful(this IEnumerable<SyncService.SyncResult> results)
```
Determines if every synchronization result in the collection is marked as successful.
- **Returns**: `true` if all results are successful; otherwise, `false`.

### `GetDuration`
```csharp
public static TimeSpan? GetDuration(this IEnumerable<SyncService.SyncResult> results)
```
Calculates the cumulative duration of all synchronization operations in the collection.
- **Returns**: A `TimeSpan` representing the total duration, or `null` if the collection is empty.

### `GetTotalChangesDetected`
```csharp
public static int GetTotalChangesDetected(this IEnumerable<SyncService.SyncResult> results)
```
Sums the total number of changes detected across all synchronization operations.
- **Returns**: The total count of detected changes as an integer.

### `GetSummary`
```csharp
public static string GetSummary(this IEnumerable<SyncService.SyncResult> results)
```
Generates a concise string summary of the synchronization collection.
- **Returns**: A summary string containing aggregated status information.

### `HasPendingConflicts`
```csharp
public static bool HasPendingConflicts(this IEnumerable<SyncService.SyncResult> results)
```
Checks if any synchronization result in the collection contains unresolved conflicts.
- **Returns**: `true` if at least one result has pending conflicts; otherwise, `false`.

### `GetCompletionPercentage`
```csharp
public static int GetCompletionPercentage(this IEnumerable<SyncService.SyncResult> results)
```
Calculates the overall completion progress of the synchronization operations as a percentage.
- **Returns**: An integer between 0 and 100 representing the completion percentage.

### `GetErrorMessage`
```csharp
public static string? GetErrorMessage(this IEnumerable<SyncService.SyncResult> results)
```
Retrieves an error message associated with failed synchronization operations.
- **Returns**: A string containing the error message if failures exist; otherwise, `null`.

### `HasSignificantChanges`
```csharp
public static bool HasSignificantChanges(this IEnumerable<SyncService.SyncResult> results)
```
Determines if the synchronization operations resulted in any significant changes.
- **Returns**: `true` if significant changes are present; otherwise, `false`.

### `GetDetailedSummary`
```csharp
public static string GetDetailedSummary(this IEnumerable<SyncService.SyncResult> results)
```
Provides a comprehensive, verbose summary of all synchronization operations.
- **Returns**: A formatted string containing detailed information for each result.

### `WhereSuccessful`
```csharp
public static IEnumerable<SyncService.SyncResult> WhereSuccessful(this IEnumerable<SyncService.SyncResult> results)
```
Filters the collection to return only successful synchronization results.
- **Returns**: An `IEnumerable<SyncService.SyncResult>` containing only successful operations.

### `WhereFailed`
```csharp
public static IEnumerable<SyncService.SyncResult> WhereFailed(this IEnumerable<SyncService.SyncResult> results)
```
Filters the collection to return only failed synchronization results.
- **Returns**: An `IEnumerable<SyncService.SyncResult>` containing only failed operations.

### `OrderByCompletion`
```csharp
public static IOrderedEnumerable<SyncService.SyncResult> OrderByCompletion(this IEnumerable<SyncService.SyncResult> results)
```
Orders the synchronization results based on their completion metrics.
- **Returns**: An `IOrderedEnumerable<SyncService.SyncResult>` ordered by completion status.

### `GetMostRecent`
```csharp
public static SyncService.SyncResult? GetMostRecent(this IEnumerable<SyncService.SyncResult> results)
```
Retrieves the most recent synchronization result from the collection.
- **Returns**: The latest `SyncService.SyncResult`, or `null` if the collection is empty.

## Usage

### Example 1: Basic Sync Status Check
```csharp
var results = syncService.GetRecentResults();

if (results.IsSuccessful())
{
    Console.WriteLine("Sync completed successfully.");
}
else
{
    Console.WriteLine($"Sync failed. Error: {results.GetErrorMessage()}");
}
```

### Example 2: Analyzing Failed Sync Operations
```csharp
var results = syncService.GetAllResults();

if (results.HasPendingConflicts())
{
    var failures = results.WhereFailed();
    foreach (var failure in failures)
    {
        Console.WriteLine($"Operation failed: {failure.Id}");
    }
}
```

## Notes

- **Null Handling**: These methods throw an `ArgumentNullException` if the `results` collection passed to them is `null`. Ensure the source collection is initialized before calling any extension methods.
- **Thread Safety**: These extension methods do not modify the underlying collection and are generally thread-safe for reading. However, if the underlying `IEnumerable<SyncService.SyncResult>` is modified by another thread during enumeration, an `InvalidOperationException` may be thrown by the underlying LINQ provider.
- **Performance**: Many of these methods iterate over the entire collection. In scenarios with a very large number of `SyncResult` objects, be mindful of the performance impact of calling these methods repeatedly within tight loops.
