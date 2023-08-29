# BulkOperationService

Provides bulk operations for managing tasks in the Notion task synchronization system. Each method executes a single type of bulk modification (e.g., status update, tag addition) and records the outcome in the service’s observable properties. The service is designed for sequential, synchronous‑like usage within a single logical operation; it is not thread‑safe for concurrent calls.

## API

### `BulkOperationService()`
Initializes a new instance of the service. All statistical properties (`Operation`, `Requested`, `Affected`, `Skipped`) are set to their default values (empty string and zero).

### `async Task<BulkResult> UpdateStatusAsync(…)`
Executes a bulk status update on a set of tasks.  
**Parameters:** A collection of task identifiers and the target status value.  
**Returns:** A `BulkResult` containing the outcome of the operation.  
**Throws:** `ArgumentNullException` if the identifier collection or status value is `null`; `InvalidOperationException` if the underlying data source is unavailable.

### `async Task<BulkResult> AddTagAsync(…)`
Adds a tag to a set of tasks.  
**Parameters:** A collection of task identifiers and the tag to add.  
**Returns:** A `BulkResult` describing the operation result.  
**Throws:** `ArgumentNullException` for null parameters; `InvalidOperationException` on data source errors.

### `async Task<BulkResult> RemoveTagAsync(…)`
Removes a tag from a set of tasks.  
**Parameters:** A collection of task identifiers and the tag to remove.  
**Returns:** A `BulkResult` with the operation details.  
**Throws:** `ArgumentNullException` for null parameters; `InvalidOperationException` on data source errors.

### `async Task<BulkResult> AssignAsync(…)`
Assigns a user (or entity) to a set of tasks.  
**Parameters:** A collection of task identifiers and the assignee identifier.  
**Returns:** A `BulkResult` indicating success or partial failure.  
**Throws:** `ArgumentNullException` for null parameters; `InvalidOperationException` on data source errors.

### `async Task<BulkResult> SetPriorityAsync(…)`
Sets the priority level for a set of tasks.  
**Parameters:** A collection of task identifiers and the priority value.  
**Returns:** A `BulkResult` with the operation statistics.  
**Throws:** `ArgumentNullException` for null parameters; `InvalidOperationException` on data source errors.

### `async Task<BulkResult> DeleteAsync(…)`
Deletes a set of tasks.  
**Parameters:** A collection of task identifiers.  
**Returns:** A `BulkResult` summarizing the deletion.  
**Throws:** `ArgumentNullException` if the identifier collection is `null`; `InvalidOperationException` on data source errors.

### `async Task<List<Domain.Models.Task>> QueryAsync(…)`
Retrieves a list of tasks that match the specified filter criteria.  
**Parameters:** A filter object or predicate that defines which tasks to return.  
**Returns:** A `List<Domain.Models.Task>` containing the matching tasks.  
**Throws:** `ArgumentNullException` if the filter is `null`; `InvalidOperationException` on data source errors.

### `string Operation { get; }`
Gets the name of the last executed bulk operation (e.g., `"UpdateStatus"`, `"AddTag"`). Empty string if no operation has been performed.

### `int Requested { get; }`
Gets the total number of tasks that were requested to be affected by the last operation.

### `int Affected { get; }`
Gets the number of tasks that were successfully affected by the last operation.

### `int Skipped { get; }`
Gets the number of tasks that were skipped (e.g., because they did not exist or the operation was not applicable) during the last operation.

## Usage

### Example 1: Bulk update task status and inspect results

```csharp
var service = new BulkOperationService();
var taskIds = new[] { "task-1", "task-2", "task-3" };
BulkResult result = await service.UpdateStatusAsync(taskIds, "Done");

Console.WriteLine($"Operation: {service.Operation}");
Console.WriteLine($"Requested: {service.Requested}");
Console.WriteLine($"Affected: {service.Affected}");
Console.WriteLine($"Skipped: {service.Skipped}");
```

### Example 2: Bulk add a tag and handle partial success

```csharp
var service = new BulkOperationService();
var taskIds = new[] { "task-4", "task-5", "task-6" };
BulkResult result = await service.AddTagAsync(taskIds, "urgent");

if (result.Skipped > 0)
{
    Console.WriteLine($"Warning: {result.Skipped} tasks were skipped.");
}
```

## Notes

- The service maintains mutable state (`Operation`, `Requested`, `Affected`, `Skipped`). These properties are overwritten on each call to a bulk method. If you need to preserve results from multiple operations, capture the `BulkResult` or the property values before the next call.
- The service is **not thread‑safe**. Concurrent calls to any of the `async` methods will produce undefined behavior (e.g., interleaved property updates). Use external synchronization (e.g., a lock) if concurrent access is required.
- An empty collection of task identifiers results in an operation with zero `Requested`, `Affected`, and `Skipped`. No exceptions are thrown for an empty collection unless the parameter itself is `null`.
- If a task identifier does not exist in the data source, it is counted as `Skipped` rather than causing the entire operation to fail. Other tasks in the same batch are still processed.
- The `QueryAsync` method does not modify the service’s statistical properties; it only returns a list of tasks.
