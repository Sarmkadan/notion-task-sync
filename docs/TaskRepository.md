# TaskRepository

`TaskRepository` provides persistence and query operations for `Task` entities within the `notion-task-sync` system. It abstracts the underlying data store, offering asynchronous methods to create, update, delete, and retrieve tasks by various criteria such as status, assignee, modification timestamp, and Notion page identifier.

## API

### Constructors

- **`TaskRepository()`**
  Initializes a new instance of the repository, establishing the necessary connection or context for data access. Details of the underlying store are encapsulated.

### Methods

- **`async global::System.Threading.Tasks.Task AddAsync(Task task)`**
  Persists a new `Task` entity.
  - **Parameters:** `task` — the `Task` object to insert. Must not be `null`.
  - **Returns:** A `Task` representing the asynchronous operation.
  - **Throws:** `ArgumentNullException` if `task` is `null`. May throw data-access exceptions on constraint violations or connection failures.

- **`async global::System.Threading.Tasks.Task UpdateAsync(Task task)`**
  Persists changes to an existing `Task` entity.
  - **Parameters:** `task` — the `Task` object with updated values. Must not be `null` and must correspond to a record already in the store.
  - **Returns:** A `Task` representing the asynchronous operation.
  - **Throws:** `ArgumentNullException` if `task` is `null`. May throw a concurrency or not-found exception if the entity no longer exists in the store.

- **`async global::System.Threading.Tasks.Task DeleteAsync(Task task)`**
  Removes a `Task` entity from the active data set (soft or hard delete depending on implementation).
  - **Parameters:** `task` — the `Task` object to delete. Must not be `null`.
  - **Returns:** A `Task` representing the asynchronous operation.
  - **Throws:** `ArgumentNullException` if `task` is `null`. May throw if the entity does not exist.

- **`async Task<Task?> GetByIdAsync(int id)`**
  Retrieves a single `Task` by its internal unique identifier.
  - **Parameters:** `id` — the integer identifier of the task.
  - **Returns:** The matching `Task`, or `null` if no task with the given `id` exists.
  - **Throws:** Data-access exceptions on connection or query failures.

- **`async Task<Task?> GetByNotionPageIdAsync(string notionPageId)`**
  Retrieves a single `Task` by its associated Notion page identifier.
  - **Parameters:** `notionPageId` — the Notion page ID string.
  - **Returns:** The matching `Task`, or `null` if no task is linked to that Notion page.
  - **Throws:** `ArgumentNullException` if `notionPageId` is `null`. Data-access exceptions on query failures.

- **`async Task<List<Task>> GetAllAsync()`**
  Retrieves all active (non-deleted) `Task` entities.
  - **Returns:** A list of `Task` objects. Returns an empty list if no tasks exist.
  - **Throws:** Data-access exceptions on query failures.

- **`async Task<List<Task>> GetByStatusAsync(TaskStatus status)`**
  Retrieves all active tasks matching the specified status.
  - **Parameters:** `status` — the `TaskStatus` value to filter by.
  - **Returns:** A list of `Task` objects with the given status. Returns an empty list if none match.
  - **Throws:** Data-access exceptions on query failures.

- **`async Task<List<Task>> GetModifiedSinceAsync(DateTime timestamp)`**
  Retrieves all active tasks whose last-modified timestamp is on or after the specified moment.
  - **Parameters:** `timestamp` — the `DateTime` cutoff (inclusive).
  - **Returns:** A list of `Task` objects modified since `timestamp`. Returns an empty list if none match.
  - **Throws:** Data-access exceptions on query failures.

- **`async Task<List<Task>> GetAssignedToAsync(string assignee)`**
  Retrieves all active tasks assigned to a specific person.
  - **Parameters:** `assignee` — the assignee identifier or name string.
  - **Returns:** A list of `Task` objects assigned to that person. Returns an empty list if none match.
  - **Throws:** `ArgumentNullException` if `assignee` is `null`. Data-access exceptions on query failures.

- **`async Task<List<Task>> GetOverdueAsync()`**
  Retrieves all active tasks whose due date is in the past and whose status indicates they are not yet completed.
  - **Returns:** A list of overdue `Task` objects. Returns an empty list if none are overdue.
  - **Throws:** Data-access exceptions on query failures.

- **`async global::System.Threading.Tasks.Task SaveAsync()`**
  Commits any pending changes tracked by the repository to the underlying store. The exact behavior depends on the unit-of-work or change-tracking pattern employed.
  - **Returns:** A `Task` representing the asynchronous save operation.
  - **Throws:** Data-access exceptions on commit failures, including concurrency conflicts.

- **`async Task<int> CountAsync()`**
  Returns the total number of active (non-deleted) tasks.
  - **Returns:** An integer count.
  - **Throws:** Data-access exceptions on query failures.

- **`async Task<Dictionary<TaskStatus, int>> CountByStatusAsync()`**
  Returns a breakdown of active task counts grouped by status.
  - **Returns:** A dictionary where each key is a `TaskStatus` and each value is the count of tasks in that status. Statuses with zero tasks may be omitted.
  - **Throws:** Data-access exceptions on query failures.

- **`async Task<List<Task>> GetAllIncludingDeletedAsync()`**
  Retrieves all `Task` entities, including those that have been soft-deleted.
  - **Returns:** A list of all `Task` objects regardless of deletion state. Returns an empty list if no tasks exist.
  - **Throws:** Data-access exceptions on query failures.

## Usage

### Example 1: Creating and retrieving a task by Notion page ID

```csharp
var repository = new TaskRepository();

var newTask = new Task
{
    Title = "Review Q3 report",
    Status = TaskStatus.InProgress,
    NotionPageId = "abc123-def456",
    AssignedTo = "alice",
    DueDate = DateTime.UtcNow.AddDays(3)
};

await repository.AddAsync(newTask);
await repository.SaveAsync();

// Later, look up the task using its Notion page ID
Task? retrieved = await repository.GetByNotionPageIdAsync("abc123-def456");
if (retrieved != null)
{
    Console.WriteLine($"Found task: {retrieved.Title} [{retrieved.Status}]");
}
```

### Example 2: Bulk status check and overdue cleanup

```csharp
var repository = new TaskRepository();

// Get a summary of all tasks by status
Dictionary<TaskStatus, int> counts = await repository.CountByStatusAsync();
foreach (var kvp in counts)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Fetch and escalate overdue tasks
List<Task> overdueTasks = await repository.GetOverdueAsync();
foreach (var task in overdueTasks)
{
    task.Status = TaskStatus.Blocked;
    task.ModifiedAt = DateTime.UtcNow;
    await repository.UpdateAsync(task);
}

await repository.SaveAsync();
```

## Notes

- **Soft-delete semantics:** Methods such as `GetAllAsync`, `GetByStatusAsync`, `GetModifiedSinceAsync`, `GetAssignedToAsync`, `GetOverdueAsync`, and `CountAsync` operate only on active (non-deleted) tasks. Use `GetAllIncludingDeletedAsync` to include soft-deleted records.
- **`SaveAsync` behavior:** If the repository employs a unit-of-work pattern, changes made via `AddAsync`, `UpdateAsync`, or `DeleteAsync` may remain pending until `SaveAsync` is called. Callers should invoke `SaveAsync` explicitly to persist modifications.
- **Null returns:** `GetByIdAsync` and `GetByNotionPageIdAsync` return `null` when no matching record is found. Callers must guard against null before accessing task members.
- **Thread safety:** This class is not guaranteed to be thread-safe. Instances should not be shared concurrently without external synchronization. Each unit of work should ideally use its own repository instance or be protected by a synchronization mechanism appropriate to the underlying data store.
- **Overdue calculation:** `GetOverdueAsync` relies on the current system time and the task’s `DueDate` and `Status`. Tasks without a `DueDate` are typically excluded from the overdue set. The exact statuses considered “not completed” depend on the `TaskStatus` enumeration definition.
- **Modified-since inclusivity:** `GetModifiedSinceAsync` uses an inclusive comparison (`>=`). Passing `DateTime.UtcNow` may return tasks modified in the same instant, depending on clock precision and store behavior.
