# TaskRepositoryExtensions

The `TaskRepositoryExtensions` class provides a suite of convenience extension methods for `ITaskRepository` implementations within the `notion-task-sync` project. These methods simplify common query operations, allowing for more concise retrieval of `Task` entities based on temporal, relational, and priority-based filtering criteria.

## API

### GetDueWithinAsync
Retrieves a list of tasks that have a due date falling within the specified duration from the current time.
- **Parameters:** 
  - `ITaskRepository repository` - The repository instance.
  - `TimeSpan timeframe` - The duration within which to search for due tasks.
- **Returns:** `Task<List<Task>>` - A list of tasks matching the criterion.
- **Throws:** `ArgumentNullException` if the repository instance is null.

### GetAssignedOverdueAsync
Retrieves all tasks that are currently overdue and assigned to a specific user.
- **Parameters:** 
  - `ITaskRepository repository` - The repository instance.
  - `string assigneeId` - The identifier of the assigned user.
- **Returns:** `Task<List<Task>>` - A list of overdue tasks for the specified user.
- **Throws:** `ArgumentNullException` if the repository instance is null or `assigneeId` is invalid.

### GetByPriorityAsync
Retrieves all tasks filtered by a specified priority level.
- **Parameters:** 
  - `ITaskRepository repository` - The repository instance.
  - `Priority priority` - The priority level to filter by.
- **Returns:** `Task<List<Task>>` - A list of tasks matching the priority level.
- **Throws:** `ArgumentNullException` if the repository instance is null.

### GetWhereAsync
Retrieves all tasks that satisfy a provided predicate.
- **Parameters:** 
  - `ITaskRepository repository` - The repository instance.
  - `Func<Task, bool> predicate` - The filtering condition.
- **Returns:** `Task<List<Task>>` - A list of tasks matching the predicate.
- **Throws:** `ArgumentNullException` if the repository instance or the predicate is null.

## Usage

```csharp
// Retrieving tasks due in the next 24 hours
var urgentTasks = await repository.GetDueWithinAsync(TimeSpan.FromHours(24));

// Retrieving tasks assigned to a user that meet a specific custom condition
var userTasks = await repository.GetWhereAsync(t => t.AssigneeId == "user-123" && t.Status == Status.InProgress);
```

## Notes

- **Thread Safety:** These extension methods are thread-safe, assuming the underlying `ITaskRepository` implementation adheres to thread-safe practices.
- **Empty Sets:** If no tasks match the criteria, these methods return an empty `List<Task>`, not `null`.
- **Repository State:** These methods rely on the state of the repository at the time of invocation. If the underlying data source is updated concurrently, results may reflect the state during the asynchronous operation.
- **Performance:** For large datasets, `GetWhereAsync` should be used with caution, as it may require local filtering depending on the `ITaskRepository` implementation.
