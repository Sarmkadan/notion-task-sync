# BackupService

`BackupService` manages the lifecycle of application backups: creation, enumeration, restoration, and deletion. It provides metadata about individual backups through `BackupInfo` and aggregate statistics through `BackupStats`. The service operates on a configurable storage path and assigns each backup a unique identifier.

## API

### BackupService

```csharp
public BackupService(string? path = null)
```

Constructs a new backup service instance. If `path` is `null` or omitted, a default storage location is used. The specified directory must exist or be creatable; otherwise, subsequent operations may fail with `DirectoryNotFoundException` or `UnauthorizedAccessException`.

---

### CreateBackupAsync

```csharp
public async Task<BackupInfo> CreateBackupAsync(string? label = null)
```

Creates a new backup asynchronously. Accepts an optional human-readable `label`. Returns a `BackupInfo` describing the resulting backup. Throws `InvalidOperationException` if the backup destination is inaccessible or if insufficient disk space prevents completion. Throws `IOException` when underlying file operations fail.

---

### GetAvailableBackups

```csharp
public List<BackupInfo> GetAvailableBackups()
```

Returns all backups currently stored at the configured path, ordered by `CreatedAt` descending (most recent first). Returns an empty list if no backups exist. Does not throw; file system errors are swallowed and result in an empty list.

---

### RestoreFromBackupAsync

```csharp
public async Task RestoreFromBackupAsync(Guid id)
```

Restores application state from the backup identified by `id`. Throws `KeyNotFoundException` if no backup with the given `id` exists. Throws `InvalidDataException` if the backup archive is corrupt or unreadable. Throws `IOException` if the restore target cannot be written.

---

### DeleteBackupAsync

```csharp
public async Task DeleteBackupAsync(Guid id)
```

Permanently removes the backup identified by `id`. Throws `KeyNotFoundException` if no backup with the given `id` exists. Throws `UnauthorizedAccessException` if the process lacks permission to delete the backup files. Throws `IOException` for other file-level failures.

---

### GetBackupStats

```csharp
public BackupStats GetBackupStats()
```

Aggregates statistics across all available backups and returns a `BackupStats` instance. If no backups exist, `BackupStats` fields reflect zero or null values. Does not throw.

---

### BackupInfo

Represents metadata for a single backup.

| Member       | Type       | Description                                      |
|--------------|------------|--------------------------------------------------|
| `Id`         | `Guid`     | Unique identifier assigned at creation.          |
| `Path`       | `string?`  | Full file system path to the backup archive.     |
| `CreatedAt`  | `DateTime` | UTC timestamp when the backup was created.       |
| `Label`      | `string?`  | Optional label supplied during creation.         |
| `FileCount`  | `int`      | Number of files contained in the backup.         |
| `GetAge`     | `TimeSpan` | Elapsed time since `CreatedAt`.                  |
| `ToString()` | `string`   | Returns a formatted string combining label, date, and size. |

---

### BackupStats

Represents aggregate backup statistics.

| Member           | Type        | Description                                          |
|------------------|-------------|------------------------------------------------------|
| `TotalBackups`   | `int`       | Total number of backups available.                   |
| `TotalSizeBytes` | `long`      | Sum of all backup file sizes in bytes.               |
| `LastBackupTime` | `DateTime?` | `CreatedAt` of the most recent backup, or `null`.    |
| `GetTotalSizeMB` | `double`    | `TotalSizeBytes` converted to megabytes (base-10).   |
| `ToString()`     | `string`    | Returns a summary string with count and total size.  |

---

## Usage

### Example 1: Creating and labeling a backup

```csharp
var service = new BackupService(@"C:\Backups\NotionSync");

BackupInfo backup = await service.CreateBackupAsync("Pre-migration snapshot");
Console.WriteLine($"Backup created: {backup.Id} at {backup.CreatedAt:u}");
Console.WriteLine($"Age: {backup.GetAge.TotalMinutes:F1} minutes ago");
```

### Example 2: Listing, restoring, and cleaning up

```csharp
var service = new BackupService();

List<BackupInfo> backups = service.GetAvailableBackups();
if (backups.Count == 0)
{
    Console.WriteLine("No backups available.");
    return;
}

// Restore the most recent backup
BackupInfo latest = backups[0];
await service.RestoreFromBackupAsync(latest.Id);
Console.WriteLine($"Restored from {latest.Label ?? latest.Id.ToString()}");

// Delete backups older than 30 days
foreach (BackupInfo info in backups.Where(b => b.GetAge.TotalDays > 30))
{
    await service.DeleteBackupAsync(info.Id);
}

BackupStats stats = service.GetBackupStats();
Console.WriteLine(stats.ToString());
```

---

## Notes

- **Thread safety:** `BackupService` instance methods are not thread-safe. Concurrent calls to `CreateBackupAsync`, `RestoreFromBackupAsync`, or `DeleteBackupAsync` on the same instance may cause race conditions on the underlying file store. Use external synchronization when sharing an instance across threads.
- **Path persistence:** The storage path is fixed at construction. Changing the backup location requires a new `BackupService` instance.
- **Empty state:** When no backups exist, `GetAvailableBackups` returns an empty list, `GetBackupStats` returns zeroed fields, and `RestoreFromBackupAsync`/`DeleteBackupAsync` throw `KeyNotFoundException` for any supplied `Guid`.
- **Partial failures:** If `CreateBackupAsync` fails mid-operation, no partial backup is retained; the operation is atomic from the caller's perspective.
- **Label uniqueness:** Labels are not required to be unique. Multiple backups may share the same label.
- **Size calculation:** `GetTotalSizeMB` uses base-10 division (bytes / 1,000,000), not base-2 mebibytes.
- **Timestamp precision:** `CreatedAt` is recorded in UTC. `GetAge` reflects the system clock at call time; clock changes may produce negative or unexpectedly large values.
