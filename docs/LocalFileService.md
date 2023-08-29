# LocalFileService

The `LocalFileService` provides a simple asynchronous API for persisting and retrieving `Task` objects to the local file system. It encapsulates file I/O operations such as saving, loading, deleting, and backing up task data, while also exposing utility members for inspecting the storage state.

## API

### `public LocalFileService()`
Initializes a new instance of the service using the default base path configured for the application. The constructor prepares the underlying directory; if it does not exist, it will be created on the first write operation.

### `public async Task SaveTaskAsync()`
Persists the current task data to storage.  
- **Parameters:** none.  
- **Return Value:** a `Task` representing the asynchronous operation.  
- **Exceptions:**  
  - `IOException` if the file cannot be written or the disk is full.  
  - `UnauthorizedAccessException` if the application lacks permission to the target directory.  
  - `ObjectDisposedException` if the service has been disposed.

### `public async Task<Task?> LoadTaskAsync()`
Loads a single `Task` instance from storage.  
- **Parameters:** none.  
- **Return Value:** a `Task` containing the loaded data, or `null` if no task file is present.  
- **Exceptions:**  
  - `IOException` on read failures.  
  - `UnauthorizedAccessException` for insufficient permissions.  
  - `ObjectDisposedException` if the service is disposed.

### `public async Task<List<Task>> LoadAllTasksAsync()`
Loads all persisted `Task` objects.  
- **Parameters:** none.  
- **Return Value:** a `List<Task>` containing every task found; an empty list if none exist.  
- **Exceptions:**  
  - `IOException` for read errors.  
  - `UnauthorizedAccessException` when access is denied.  
  - `ObjectDisposedException` if the service has been disposed.

### `public async Task DeleteTaskAsync()`
Removes the task file associated with the current context.  
- **Parameters:** none.  
- **Return Value:** a `Task` representing the asynchronous deletion.  
- **Exceptions:**  
  - `FileNotFoundException` if no task file exists to delete.  
  - `IOException` for general deletion failures.  
  - `UnauthorizedAccessException` when lacking delete rights.  
  - `ObjectDisposedException` if the service is disposed.

### `public async Task<string> BackupTasksAsync()`
Creates a backup of all task files and returns the path to the backup archive.  
- **Parameters:** none.  
- **Return Value:** a `Task<string>` that resolves to the full file path of the backup.  
- **Exceptions:**  
  - `IOException` if the backup cannot be created (e.g., insufficient space).  
  - `UnauthorizedAccessException` for permission issues.  
  - `ObjectDisposedException` if the service is disposed.

### `public DateTime GetLastModifiedTime()`
Retrieves the timestamp of the most recent modification to any task file.  
- **Parameters:** none.  
- **Return Value:** a `DateTime` indicating the last write time; returns `DateTime.MinValue` if no files exist.  
- **Exceptions:** None under normal operation; may throw `ObjectDisposedException` if the service is disposed.

### `public int CountTaskFiles()`
Returns the number of individual task files stored in the base directory.  
- **Parameters:** none.  
- **Return Value:** an `int` count of files matching the service’s naming convention.  
- **Exceptions:** None under normal operation; may throw `ObjectDisposedException` if the service is disposed.

### `public string GetBasePath()`
Gets the root directory used by the service for all file operations.  
- **Parameters:** none.  
- **Return Value:** a `string` containing the absolute path to the storage folder.  
- **Exceptions:** None under normal operation; may throw `ObjectDisposedException` if the service is disposed.

## Usage

### Example 1: Saving and loading a single task
```csharp
using var service = new LocalFileService();

// Assume `myTask` is a populated Task instance
await service.SaveTaskAsync();   // persists the current task data

Task? loaded = await service.LoadTaskAsync();
if (loaded != null)
{
    Console.WriteLine($"Loaded task: {loaded.Title}");
}
else
{
    Console.WriteLine("No task found.");
}
```

### Example 2: Backing up all tasks and checking storage stats
```csharp
await using var service = new LocalFileService();

// Backup all tasks to a zip file
string backupPath = await service.BackupTasksAsync();
Console.WriteLine($"Backup created at: {backupPath}");

// Report storage statistics
int fileCount = service.CountTaskFiles();
DateTime lastModified = service.GetLastModifiedTime();
Console.WriteLine($"Stored {fileCount} task files, last modified {lastModified}");
```

## Notes
- The service assumes exclusive access to its base directory; concurrent calls from multiple threads or processes may result in race conditions, leading to incomplete writes or read inconsistencies. External synchronization is required for thread‑safe usage.  
- All I/O‑related methods propagate `IOException` and `UnauthorizedAccessException` callers should handle these to respond to disk‑full, permission‑missing, or file‑locking scenarios.  
- If the base directory does not exist, the service will attempt to create it on the first write operation; failure to do so will surface as an `UnauthorizedAccessException`.  
- `LoadTaskAsync` returns `null` when no task file matches the expected naming pattern; callers should verify the result before dereferencing.  
- The backup operation produces a timestamped archive; the returned path points to a newly created file that the caller is responsible for managing (e.g., cleaning up old backups).  
- Disposing the service (via `using` or await‑using) releases any held file handles; subsequent calls after disposal will throw `ObjectDisposedException`.
