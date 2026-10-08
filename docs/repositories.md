# Repositories

This document describes the three repository components used by `notion-task-sync`:

- `ITaskRepository` / `TaskRepository` (namespace `NotionTaskSync.Data.Repositories`)
- `IChangeLogRepository` / `ChangeLogRepository` (namespace `NotionTaskSync.Data.Repositories`)
- `ConfigRepository` (namespace `NotionTaskSync.Repositories`)

All three are currently **in-memory or file-based** implementations. The `Save*` methods on the in-memory repositories only reset a dirty flag; they do not persist anything. See the "Persistence status" section below.

## Overview

| Component | Kind | Storage | Entity | Interface |
|---|---|---|---|---|
| `TaskRepository` | class | In-memory `List<Task>` | `NotionTaskSync.Domain.Models.Task` | `ITaskRepository` |
| `ChangeLogRepository` | class | In-memory `List<ChangeLog>` | `NotionTaskSync.Domain.Models.ChangeLog` | `IChangeLogRepository` |
| `ConfigRepository` | class | One JSON file per config in a directory | `NotionTaskSync.Domain.Models.SyncConfig` | none (concrete class only) |

All asynchronous methods are implemented with `Task.FromResult` or `Task.CompletedTask`, so they complete synchronously.

Note on naming: inside these files `Task` refers to the domain model. The code uses `System.Threading.Tasks.Task` (or `global::System.Threading.Tasks.Task`) for the async return type.

---

## ITaskRepository / TaskRepository

### Interface contract

| Member | Signature | Purpose |
|---|---|---|
| `AddAsync` | `Task AddAsync(Task task)` | Adds a task. |
| `UpdateAsync` | `Task UpdateAsync(Task task)` | Replaces the stored task that has the same `Id`. |
| `DeleteAsync` | `Task DeleteAsync(Guid taskId)` | Removes a task by `Id`. |
| `GetByIdAsync` | `Task<Task?> GetByIdAsync(Guid taskId)` | Returns a non-deleted task by `Id`. |
| `GetByNotionPageIdAsync` | `Task<Task?> GetByNotionPageIdAsync(string notionPageId)` | Returns a non-deleted task by its Notion page ID. |
| `GetAllAsync` | `Task<List<Task>> GetAllAsync()` | All non-deleted tasks. |
| `GetByStatusAsync` | `Task<List<Task>> GetByStatusAsync(TaskStatus status)` | Non-deleted tasks with the given status. |
| `GetModifiedSinceAsync` | `Task<List<Task>> GetModifiedSinceAsync(DateTime since)` | Non-deleted tasks with `UpdatedAt >= since`. |
| `GetAssignedToAsync` | `Task<List<Task>> GetAssignedToAsync(string assignee)` | Non-deleted tasks with `AssignedTo == assignee`. |
| `GetOverdueAsync` | `Task<List<Task>> GetOverdueAsync(DateTime beforeDate)` | Non-deleted tasks with `DueDate < beforeDate` and status not `Done`. |
| `SaveAsync` | `Task SaveAsync()` | Clears the pending-changes flag. |
| `CountAsync` | `Task<int> CountAsync()` | Number of non-deleted tasks. |
| `CountByStatusAsync` | `Task<Dictionary<TaskStatus, int>> CountByStatusAsync()` | Count of non-deleted tasks grouped by status. |

### TaskRepository behavior

- **Storage:** a private `List<Task>` created in the constructor. Nothing is written to disk.
- **`AddAsync`:** throws `ArgumentNullException` if `task` is `null`. Calls `task.Validate()` and throws `InvalidOperationException("Task validation failed")` if it returns `false`. Otherwise appends the task and sets the pending flag.
- **`UpdateAsync`:** throws `ArgumentNullException` for `null`. Looks up the stored task by `Id`; throws `InvalidOperationException` if it is not found. Validates the new value (same exception as `AddAsync`). Replaces the stored item at the same index and sets the pending flag.
- **`DeleteAsync`:** looks up the task by `Id` among **all** stored tasks, including soft-deleted ones, and throws `InvalidOperationException` if it is not found. Removes it from the list (hard delete) and sets the pending flag.
- **Soft delete:** the repository treats `Task.IsDeleted == true` as hidden. Every read method except `DeleteAsync` filters on `!IsDeleted`.
- **`GetByNotionPageIdAsync`:** returns `null` for a null or empty argument without querying.
- **`GetAssignedToAsync`:** returns an empty list for a null or empty argument.
- **`GetOverdueAsync`:** requires `DueDate` to have a value, and excludes tasks whose status is `TaskStatus.Done`.
- **`SaveAsync`:** only resets `HasPendingChanges` to `false`. There is no persistence yet.
- **`CountByStatusAsync`:** returns only statuses that have at least one non-deleted task. Missing statuses are not present as zero entries.

### Members outside the interface

These are public on `TaskRepository` but not declared on `ITaskRepository`. Callers that depend only on the interface cannot use them.

- `Task<List<Task>> GetAllIncludingDeletedAsync()`: returns every stored task, including soft-deleted ones. Intended for administrative use.
- `bool HasPendingChanges { get; }`: `true` after any add, update, or delete, until `SaveAsync` is called.

### Extension methods

`Data/Repositories/TaskRepositoryExtensions.cs` contains extension methods for `TaskRepository`. See `docs/TaskRepositoryExtensions.md` for details.

---

## IChangeLogRepository / ChangeLogRepository

### Interface contract

| Member | Signature | Purpose |
|---|---|---|
| `AddAsync` | `Task AddAsync(ChangeLog changeLog)` | Appends an audit entry. |
| `GetByTaskIdAsync` | `Task<List<ChangeLog>> GetByTaskIdAsync(Guid taskId, int limit = 100)` | Entries for one task, newest first, at most `limit`. |
| `GetByDateRangeAsync` | `Task<List<ChangeLog>> GetByDateRangeAsync(DateTime from, DateTime to)` | Entries with `from <= Timestamp <= to`, newest first. |
| `GetBySourceAsync` | `Task<List<ChangeLog>> GetBySourceAsync(ChangeSource source)` | Entries from one source, newest first. |
| `GetByChangeTypeAsync` | `Task<List<ChangeLog>> GetByChangeTypeAsync(string changeType)` | Entries with `ChangeType == changeType`, newest first. |
| `GetConflictChangesAsync` | `Task<List<ChangeLog>> GetConflictChangesAsync()` | Entries with `IsConflict == true`, newest first. |
| `GetLatestAsync` | `Task<List<ChangeLog>> GetLatestAsync(int limit = 50)` | The newest `limit` entries across all tasks. |
| `CountAsync` | `Task<int> CountAsync()` | Total number of entries. |
| `CountConflictsAsync` | `Task<int> CountConflictsAsync()` | Number of entries with `IsConflict == true`. |
| `SaveAsync` | `Task SaveAsync()` | Clears the pending-changes flag. |

### ChangeLogRepository behavior

- **Storage:** a private `List<ChangeLog>`. Nothing is written to disk.
- **`AddAsync`:** throws `ArgumentNullException` if `changeLog` is `null`. Calls `changeLog.Validate()` and throws `InvalidOperationException("Change log validation failed")` if it returns `false`. Otherwise appends the entry and sets the pending flag.
- **Ordering:** every query that returns a list orders by `Timestamp` descending. `GetLatestAsync` and `GetByTaskIdAsync` apply `Take(limit)` after ordering.
- **`GetByChangeTypeAsync`:** returns an empty list for a null or empty argument. Matching is an exact string comparison, and the code uses the literal values `"Created"`, `"Updated"`, and `"Deleted"` in the stats method.
- **`SaveAsync`:** only resets the pending flag. No persistence.

### Members outside the interface

- `Task<List<ChangeLog>> GetFullAuditTrailAsync(Guid taskId)`: calls `GetByTaskIdAsync(taskId, int.MaxValue)`.
- `Task<ChangeLogStats> GetStatsAsync()`: computes totals over the whole log. Returns a `ChangeLogStats` object with these properties:
  - `TotalChanges`
  - `LocalChanges`, `NotionChanges`, `SystemChanges` (counted by `ChangeSource`)
  - `ConflictCount`
  - `CreatedCount`, `UpdatedCount`, `DeletedCount` (counted by the literal `ChangeType` strings above)
- `bool HasPendingChanges { get; }`

`ChangeLogStats` is defined in the same file as `ChangeLogRepository`. Its `ToString()` returns a short summary line.

---

## ConfigRepository

`ConfigRepository` is a concrete class with no interface. It stores each `SyncConfig` as a separate JSON file in a configuration directory. This makes profiles easy to copy, back up, and version-control. It lives in namespace `NotionTaskSync.Repositories`, not `NotionTaskSync.Data.Repositories`.

### Constructor

```csharp
public ConfigRepository(
    string configDirectory,
    FileSystemHelper fileSystemHelper,
    JsonFormatter jsonFormatter,
    ILogger<ConfigRepository> logger)
```

- Throws `ArgumentNullException` if any argument is `null`.
- Calls `fileSystemHelper.EnsureDirectoryExists(configDirectory)`. Constructing the repository therefore creates the directory if it does not exist.

### Methods

| Method | Returns | Behavior |
|---|---|---|
| `SaveConfigAsync(SyncConfig config)` | `Task<bool>` | Writes `config` to `<sanitized Name>.json` in the config directory. Overwrites an existing file with the same name. Returns `false` on failure. |
| `GetConfigAsync(string configName)` | `Task<SyncConfig?>` | Reads `<sanitized configName>.json`. Returns `null` if the file is missing or if reading or parsing throws. |
| `GetAllConfigsAsync()` | `Task<List<SyncConfig>>` | Reads every `*.json` file in the config directory. Returns an empty list if the directory is missing or on error. |
| `DeleteConfig(string configName)` | `bool` | Deletes `<sanitized configName>.json`. Returns `true` if the file system helper reports success. |
| `ExportConfigAsync(SyncConfig config, string exportPath)` | `Task<bool>` | Writes the serialized config to an arbitrary path. Does not touch the config directory. |
| `ImportConfigAsync(string importPath)` | `Task<SyncConfig?>` | Reads and parses a config file from an arbitrary path. If parsing succeeds, saves it into the config directory with `SaveConfigAsync`. Returns `null` if the file is missing or on error. |
| `ConfigExists(string configName)` | `bool` | Checks whether `<sanitized configName>.json` is an existing file. |

### Behavior notes

- **File naming:** the file name is `configName.SanitizeForFilename() + ".json"`. The sanitizing helper removes characters that are invalid in file names. Two names that sanitize to the same value refer to the same file.
- **Error handling:** `SaveConfigAsync`, `GetConfigAsync`, `GetAllConfigsAsync`, `DeleteConfig`, `ExportConfigAsync`, and `ImportConfigAsync` catch all exceptions, log them through `ILogger`, and return a failure value (`false`, `null`, or an empty list). They do not rethrow. Callers must check the return value.
- **Logging:** successful saves, loads, deletes, exports, and imports are logged at `Information` level. Failures are logged at `Error` level. A missing import file is logged at `Warning` level.
- **Partial failure in `GetAllConfigsAsync`:** one unreadable file aborts the whole loop, so the method returns only the configs read before the failure.
- **Import overwrites:** `ImportConfigAsync` saves into the config directory without checking `ConfigExists`. An existing profile with the same name is overwritten.

---

## Persistence status

| Repository | Current behavior | Limitation |
|---|---|---|
| `TaskRepository` | Stores tasks in memory. `SaveAsync` resets a flag. | Data is lost when the process exits. |
| `ChangeLogRepository` | Stores audit entries in memory. `SaveAsync` resets a flag. | Audit history is lost when the process exits. |
| `ConfigRepository` | Writes JSON files to disk. | Only file-based persistence in this set. |

The in-memory repositories are not thread-safe. `List<T>` is accessed without locking, so concurrent use from multiple threads is not supported.
