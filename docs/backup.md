# Backup and Restore Flow

`BackupService` (`Services/BackupService.cs`) creates, lists, restores, and prunes snapshots of the local task files. Reading and writing the task files themselves is done by [`LocalFileService`](LocalFileService.md), which `BackupService` receives through its constructor. This page describes the flow as implemented in the code.

## Components

| Type | Role |
|---|---|
| `BackupService` | Orchestrates backups: naming, retention, listing, restore, deletion. |
| `LocalFileService` | Owns the task directory (`GetBasePath()`) and copies task files into a backup folder (`BackupTasksAsync`). |
| `BackupInfo` | Describes one backup: `Path`, `CreatedAt`, `Label`, `FileCount`. |
| `BackupStats` | Aggregates: `TotalBackups`, `TotalSizeBytes`, `LastBackupTime`. |
| `SyncException` | Wraps I/O failures from create, restore, and delete. |

The constructor is:

```csharp
public BackupService(string backupDirectory, int maxBackupFiles, LocalFileService fileService)
```

- `backupDirectory` is where backup folders are created and listed.
- `maxBackupFiles` is the retention limit (number of backups to keep).
- `fileService` supplies the task directory and performs the copy during backup.

Both `backupDirectory` and `fileService` must be non-null; otherwise the constructor throws `ArgumentNullException`.

## On-disk layout

```
<backupDirectory>/
└── backup_20261025_101530_auto/        <- BackupService: "backup_{yyyyMMdd_HHmmss}_{label}"
    └── tasks_backup_20261025_101530/   <- LocalFileService: "tasks_backup_{yyyyMMdd_HHmmss}"
        ├── task-a.md
        └── task-b.md
```

- The **outer** folder is the unit of listing, retention, and deletion. Its name encodes the timestamp and label.
- The **inner** folder is created by `LocalFileService.BackupTasksAsync` and holds the copied `*.md` task files.
- Only `*.md` files are copied. The label defaults to `auto` when `CreateBackupAsync` is called without one.

## Create flow: `CreateBackupAsync(label)`

1. `EnsureBackupDirectoryExists()` creates `backupDirectory` if it is missing.
2. A UTC timestamp is formatted as `yyyyMMdd_HHmmss`. The outer path is `backup_{timestamp}_{label}`.
3. `Directory.CreateDirectory` creates the outer folder.
4. `LocalFileService.BackupTasksAsync(outerPath)`:
   - If the task directory does not exist, it returns `string.Empty` and nothing is copied.
   - Otherwise it creates `tasks_backup_{timestamp}` inside the outer folder, copies every `*.md` file from `GetBasePath()` into it with overwrite, and returns the inner path.
5. A `BackupInfo` is built with a new `Id`, `Path` set to the value returned in step 4 (the **inner** folder), `CreatedAt`, `Label`, and `FileCount` (number of files in the inner folder, or `0` if the path is empty).
6. `CleanupOldBackupsAsync()` runs retention (see below).
7. The `BackupInfo` is returned.

Any exception in steps 1 to 5 is rethrown as `SyncException("Failed to create backup: ...")`. Retention errors are swallowed and do not fail the call.

> Note: when the task directory is missing, the outer folder is still created and left empty, and the returned `BackupInfo.Path` is `""`.

## Retention: `CleanupOldBackupsAsync()`

- Calls `GetAvailableBackups()`, which is ordered newest first by `CreationTimeUtc`.
- If there are more than `maxBackupFiles` entries, every entry after the first `maxBackupFiles` is passed to `DeleteBackupAsync`.
- Any exception is ignored, so a failed prune never breaks backup creation.

Retention runs only after a successful create. It does not run on its own.

## List and stats

- `GetAvailableBackups()` lists each **outer** folder under `backupDirectory` as a `BackupInfo` with `Path` = outer folder, `CreatedAt` = directory creation time (UTC), and `Label` taken from the folder name (the text after `backup_{yyyyMMdd_HHmmss}_`). If the name does not match that pattern, the full folder name is used as the label. It returns an empty list when the directory is missing and swallows errors.
- `GetBackupStats()` counts the outer folders for `TotalBackups`, sums the size of every file beneath them (recursive) for `TotalSizeBytes`, and sets `LastBackupTime` from the newest entry of `GetAvailableBackups()`. It swallows errors and returns whatever it has collected.

Because `FileCount` in `GetAvailableBackups()` counts files directly inside the outer folder, it reports `0` for backups created by `CreateBackupAsync`. The real file count is in the inner folder, which `CreateBackupAsync` reports in its return value.

## Restore flow: `RestoreFromBackupAsync(backupPath)`

1. `backupPath` must be non-null (`ArgumentNullException`).
2. If `backupPath` does not exist, an `InvalidOperationException` is raised and wrapped in `SyncException("Failed to restore from backup: ...")`.
3. `Directory.GetFiles(backupPath, "*.md")` lists the task files in that folder.
4. `targetDir = _fileService.GetBasePath()`. The directory is created if missing.
5. Each file is copied to `targetDir` with `File.Copy(..., overwrite: true)`. Files with the same name are overwritten. Files that exist only in the target are left alone.

Restore copies files with `File.Copy` directly. It uses `LocalFileService` only to obtain the target directory via `GetBasePath()`.

### Which path to pass

Restore expects the folder that directly contains the `*.md` files, which is the **inner** `tasks_backup_*` folder:

- `CreateBackupAsync(...).Path` returns the inner folder, so it can be passed to restore as is.
- `GetAvailableBackups()[i].Path` returns the **outer** folder. Passing it to restore finds no `*.md` files directly inside it, so the restore copies nothing and does not report an error.

Callers that restore from `GetAvailableBackups()` must resolve the inner `tasks_backup_*` folder first, or the restore becomes a silent no-op.

## Deletion: `DeleteBackupAsync(backupPath)`

- Throws `ArgumentNullException` for a null path.
- If the folder exists, it is deleted recursively. If it does not exist, nothing happens.
- Errors are wrapped in `SyncException("Failed to delete backup: ...")`.

`DeleteBackupAsync` takes the path of whatever folder it is given. Retention passes outer folders from `GetAvailableBackups()`, so the entire backup, including its inner folder, is removed.

## Error summary

| Operation | Behavior on failure |
|---|---|
| `CreateBackupAsync` | Throws `SyncException`. Retention errors are swallowed. |
| `GetAvailableBackups` | Returns the partial or empty list. Never throws. |
| `RestoreFromBackupAsync` | Throws `SyncException`. Missing source folder is an error; a folder with no `*.md` files is not. |
| `DeleteBackupAsync` | Throws `SyncException`. Missing folder is not an error. |
| `GetBackupStats` | Returns partial or empty stats. Never throws. |

`LocalFileService.BackupTasksAsync` wraps its own failures in `LocalFileException`. `BackupService` catches that and rethrows it as `SyncException`.

## Related

- [LocalFileService](LocalFileService.md): task file storage, `BackupTasksAsync`, `GetBasePath`.
- [LocalFileServiceTests](LocalFileServiceTests.md)
- [BackupServiceTests](BackupServiceTests.md)
- [ARCHITECTURE](ARCHITECTURE.md)
