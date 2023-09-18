# BackupServiceExtensions

`BackupServiceExtensions` provides a collection of static extension methods for the `IBackupService` interface, streamlining the management and retrieval of application backups. These methods simplify common operations such as initiating daily backups, querying backups based on temporal or structural metadata, and performing high-level audits of the backup repository state.

## API

All methods are static extension methods for `IBackupService`.

### `CreateDailyBackupAsync`
Initiates the creation of a daily backup.
*   **Returns:** `Task<BackupInfo>` representing the created backup.
*   **Throws:** `System.IO.IOException` if the backup fails, `ArgumentNullException` if the service instance is null.

### `GetLatestBackup`
Retrieves the most recently created backup.
*   **Returns:** `BackupInfo?` (the latest backup, or null if no backups exist).

### `HasBackupWithLabel`
Checks if at least one backup exists with the specified label.
*   **Parameters:** `string label`
*   **Returns:** `bool` (true if a match is found, otherwise false).

### `GetTotalFileCount`
Calculates the sum of all files contained within all managed backups.
*   **Returns:** `long` (the total file count).

### `GetTotalAge`
Calculates the combined duration of all backups.
*   **Returns:** `TimeSpan` (the total age).

### `GetBackupsByLabelPattern`
Retrieves all backups whose labels match the provided pattern.
*   **Parameters:** `string pattern`
*   **Returns:** `List<BackupInfo>` (a list of matching backups).

### `GetBackupsInRange`
Retrieves backups created within a specific date range.
*   **Parameters:** `DateTime start`, `DateTime end`
*   **Returns:** `List<BackupInfo>` (a list of backups within the range).

### `GetOldestBackup`
Retrieves the earliest created backup.
*   **Returns:** `BackupInfo?` (the oldest backup, or null if no backups exist).

### `HasBackups`
Checks if the backup store contains any entries.
*   **Returns:** `bool` (true if backups exist, otherwise false).

### `GetBackupById`
Retrieves a specific backup by its unique identifier.
*   **Parameters:** `string id`
*   **Returns:** `BackupInfo?` (the matching backup, or null if not found).

### `GetBackupsByFileCountDescending`
Retrieves all backups, sorted by file count in descending order.
*   **Returns:** `List<BackupInfo>` (the sorted list).

### `GetBackupsByAgeAscending`
Retrieves all backups, sorted by age in ascending order.
*   **Returns:** `List<BackupInfo>` (the sorted list).

## Usage

### Creating a Daily Backup
```csharp
public async Task PerformBackupAsync(IBackupService backupService)
{
    try
    {
        var backup = await backupService.CreateDailyBackupAsync();
        Console.WriteLine($"Backup created with ID: {backup.Id}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Backup failed: {ex.Message}");
    }
}
```

### Analyzing Backups by File Count
```csharp
public void LogBackupStatistics(IBackupService backupService)
{
    if (backupService.HasBackups())
    {
        var backups = backupService.GetBackupsByFileCountDescending();
        var largest = backups.First();
        Console.WriteLine($"Largest backup has {largest.FileCount} files.");
    }
}
```

## Notes

*   **Thread Safety:** While these extension methods are static and stateless, they depend on the underlying `IBackupService` implementation. Consult the specific implementation documentation to determine thread safety guarantees for the backup store.
*   **Null Checks:** The extension methods internally validate that the `IBackupService` instance is not null. Passing a null instance will result in an `ArgumentNullException`.
*   **I/O Operations:** Methods involving file system inspection or backup creation (e.g., `CreateDailyBackupAsync`, `GetTotalFileCount`) may perform synchronous or asynchronous I/O and can throw `IOException` or unauthorized access exceptions depending on the environment.
