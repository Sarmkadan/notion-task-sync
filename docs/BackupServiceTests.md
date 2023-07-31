# BackupServiceTests
The `BackupServiceTests` class is designed to test the functionality of the `BackupService` class, ensuring that it correctly creates backups, retrieves available backups, and handles exceptions as expected. This class provides a comprehensive set of tests to validate the behavior of the `BackupService` class under various scenarios.

## API
The `BackupServiceTests` class contains the following public members:
* `BackupServiceTests`: The constructor for the `BackupServiceTests` class.
* `Dispose`: Disposes of the resources used by the `BackupServiceTests` instance.
* `CreateBackupAsync_WithValidInput_CreatesBackupDirectory`: Tests that the `CreateBackupAsync` method creates a backup directory with valid input.
* `CreateBackupAsync_WithLabel_IncludesLabelInBackupInfo`: Tests that the `CreateBackupAsync` method includes the label in the backup information when a label is provided.
* `CreateBackupAsync_WithoutLabel_UsesDefaultAutoLabel`: Tests that the `CreateBackupAsync` method uses the default auto-label when no label is provided.
* `GetAvailableBackups_WithMultipleBackups_ReturnsAllBackups`: Tests that the `GetAvailableBackups` method returns all available backups when multiple backups exist.
* `GetAvailableBackups_WithEmptyBackupDirectory_ReturnsEmptyList`: Tests that the `GetAvailableBackups` method returns an empty list when the backup directory is empty.
* `GetAvailableBackups_IncludesFileCountForEachBackup`: Tests that the `GetAvailableBackups` method includes the file count for each backup.
* `GetAvailableBackups_WithNonExistentBackupDirectory_ReturnsEmptyList`: Tests that the `GetAvailableBackups` method returns an empty list when the backup directory does not exist.
* `CreateBackupAsync_InvokesBackupTasksOnFileService`: Tests that the `CreateBackupAsync` method invokes the backup tasks on the file service.
* `CreateBackupAsync_WithFileServiceThrowingException_ThrowsSyncException`: Tests that the `CreateBackupAsync` method throws a `SyncException` when the file service throws an exception.
* `CreateBackupAsync_CreatesBackupWithCorrectTimestamp`: Tests that the `CreateBackupAsync` method creates a backup with the correct timestamp.
* `GetAvailableBackups_OrdersByCreationDateDescending`: Tests that the `GetAvailableBackups` method orders the backups by creation date in descending order.
* `CreateBackupAsync_PopulatesBackupInfoWithCorrectData`: Tests that the `CreateBackupAsync` method populates the backup information with the correct data.

## Usage
The following examples demonstrate how to use the `BackupServiceTests` class:
```csharp
// Example 1: Creating a backup with a label
var backupService = new BackupService();
var backupServiceTests = new BackupServiceTests();
await backupServiceTests.CreateBackupAsync_WithLabel_IncludesLabelInBackupInfo("My Label");

// Example 2: Retrieving available backups
var backupService = new BackupService();
var backupServiceTests = new BackupServiceTests();
var availableBackups = backupServiceTests.GetAvailableBackups_WithMultipleBackups_ReturnsAllBackups();
foreach (var backup in availableBackups)
{
    Console.WriteLine($"Backup: {backup.Name}, File Count: {backup.FileCount}");
}
```

## Notes
The `BackupServiceTests` class is designed to be thread-safe, allowing multiple tests to run concurrently without interfering with each other. However, it is essential to note that the `Dispose` method should be called after each test to ensure that resources are properly released. Additionally, the `CreateBackupAsync` method may throw a `SyncException` if the file service throws an exception, and the `GetAvailableBackups` method may return an empty list if the backup directory does not exist or is empty. These edge cases should be considered when using the `BackupService` class in a production environment.
