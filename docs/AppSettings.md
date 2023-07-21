# AppSettings

The `AppSettings` class serves as the central configuration container for the `notion-task-sync` application, encapsulating runtime parameters required for task synchronization, logging, backup strategies, and API communication. It aggregates file system paths, behavioral flags, concurrency limits, and environment-specific metadata into a single serializable object, facilitating consistent configuration management across different execution environments and sync profiles.

## API

### LocalTasksDirectory
*   **Type**: `string`
*   **Purpose**: Specifies the absolute or relative file system path where local task data files are stored and monitored for changes.
*   **Remarks**: This directory must exist and be writable by the application process; failure to access this path typically results in initialization errors during the sync startup phase.

### LogLevel
*   **Type**: `string`
*   **Purpose**: Defines the minimum severity level for log entries (e.g., "Debug", "Info", "Warning", "Error").
*   **Remarks**: Invalid string values may default to a standard level or cause configuration validation failures depending on the logging provider implementation.

### EnableConsoleLogging
*   **Type**: `bool`
*   **Purpose**: Toggles the output of log messages to the standard console output stream.
*   **Remarks**: When set to `false`, console output is suppressed regardless of the `LogLevel` setting.

### LogFilePath
*   **Type**: `string?`
*   **Purpose**: Optional path to a specific file where logs should be persisted.
*   **Remarks**: If `null`, file-based logging is disabled. If provided, the directory must be writable; otherwise, an `IOException` may be thrown during logger initialization.

### DefaultSyncIntervalSeconds
*   **Type**: `int`
*   **Purpose**: Sets the default time interval, in seconds, between automatic synchronization cycles.
*   **Remarks**: Values less than or equal to zero may be treated as invalid by the scheduler, potentially disabling automatic syncing or throwing a validation exception.

### DefaultConflictStrategy
*   **Type**: `string`
*   **Purpose**: Determines the default resolution strategy when a conflict is detected between local and remote task states (e.g., "LocalWins", "RemoteWins", "Manual").
*   **Remarks**: Unsupported strategy strings may cause runtime errors when a conflict occurs if not validated at startup.

### MaxConcurrentSyncs
*   **Type**: `int`
*   **Purpose**: Limits the maximum number of synchronization operations that can execute in parallel.
*   **Remarks**: Must be a positive integer; values less than 1 typically result in an `ArgumentOutOfRangeException` during service configuration.

### EnableChangeTracking
*   **Type**: `bool`
*   **Purpose**: Enables or disables the internal mechanism that tracks file and state changes to optimize incremental syncs.
*   **Remarks**: Disabling this may force full synchronization scans, significantly impacting performance on large datasets.

### MaxRetries
*   **Type**: `int`
*   **Purpose**: Specifies the maximum number of retry attempts for failed API calls or transient network errors before failing the operation.
*   **Remarks**: A value of 0 implies no retries; negative values are generally invalid.

### ApiTimeoutSeconds
*   **Type**: `int`
*   **Purpose**: Defines the timeout duration, in seconds, for HTTP requests made to the Notion API.
*   **Remarks**: Low values may cause premature `TaskCanceledException` or timeout errors on slow networks or during heavy payload transfers.

### BackupDirectory
*   **Type**: `string?`
*   **Purpose**: Optional path specifying the location where backup snapshots of task data are stored.
*   **Remarks**: Required if `EnableAutoBackup` is `true`. If the directory does not exist, the application may attempt to create it or throw an exception depending on implementation.

### EnableAutoBackup
*   **Type**: `bool`
*   **Purpose**: Toggles the automatic creation of data backups prior to synchronization events.
*   **Remarks**: Enabling this without a valid `BackupDirectory` usually results in a configuration validation error.

### BackupFrequencyHours
*   **Type**: `int`
*   **Purpose**: Sets the frequency, in hours, at which automatic backups are performed if enabled.
*   **Remarks**: Must be greater than 0; values exceeding logical limits (e.g., integer overflow) are not validated by the type itself but by the scheduler.

### MaxBackupFiles
*   **Type**: `int`
*   **Purpose**: Limits the number of backup files retained in the backup directory; older files are purged when this limit is exceeded.
*   **Remarks**: A value of 0 or less may disable the retention policy, leading to unbounded disk usage.

### Version
*   **Type**: `string`
*   **Purpose**: Reports the current version of the application or configuration schema.
*   **Remarks**: Typically read-only in runtime contexts, used for diagnostics and compatibility checks.

### Environment
*   **Type**: `string`
*   **Purpose**: Indicates the current runtime environment (e.g., "Development", "Staging", "Production").
*   **Remarks**: Used to conditionally apply settings or enable debug features specific to an environment.

### SyncProfiles
*   **Type**: `Dictionary<string, object>`
*   **Purpose**: Contains a collection of named synchronization profiles, allowing for heterogeneous configuration sets for different task groups.
*   **Remarks**: The structure of the `object` values is dependent on the specific profile schema; deserialization errors may occur if the content does not match expected types.

### Validate
*   **Type**: `bool`
*   **Purpose**: A flag indicating whether the settings instance has passed validation checks or triggering a validation routine depending on context.
*   **Remarks**: In many contexts, this property reflects the result of a prior validation step rather than acting as a command.

### ToString()
*   **Signature**: `public override string ToString()`
*   **Purpose**: Returns a string representation of the `AppSettings` instance, typically summarizing key configuration values for logging or debugging.
*   **Returns**: A formatted string containing property names and values.
*   **Throws**: Does not typically throw exceptions unless a specific property getter causes an error.

## Usage

### Example 1: Initializing Settings from Code
The following example demonstrates programmatically constructing an `AppSettings` instance for a production environment with strict concurrency limits and auto-backup enabled.

```csharp
var settings = new AppSettings
{
    LocalTasksDirectory = "/var/data/notion-tasks",
    LogLevel = "Warning",
    EnableConsoleLogging = false,
    LogFilePath = "/var/log/notion-sync/app.log",
    DefaultSyncIntervalSeconds = 300,
    DefaultConflictStrategy = "RemoteWins",
    MaxConcurrentSyncs = 4,
    EnableChangeTracking = true,
    MaxRetries = 3,
    ApiTimeoutSeconds = 30,
    BackupDirectory = "/var/backups/notion-sync",
    EnableAutoBackup = true,
    BackupFrequencyHours = 24,
    MaxBackupFiles = 10,
    Environment = "Production",
    Version = "1.0.0",
    SyncProfiles = new Dictionary<string, object>
    {
        { "Personal", new { Filter = "Tag:Home" } },
        { "Work", new { Filter = "Tag:Office" } }
    },
    Validate = true
};

Console.WriteLine(settings.ToString());
```

### Example 2: Conditional Configuration Based on Environment
This example shows how to adjust specific properties like logging and timeouts based on the detected environment before starting the sync process.

```csharp
public void ConfigureApp(AppSettings settings)
{
    if (settings.Environment == "Development")
    {
        settings.LogLevel = "Debug";
        settings.EnableConsoleLogging = true;
        settings.ApiTimeoutSeconds = 60; // Longer timeout for debugging
        settings.Validate = true;
    }
    else if (settings.Environment == "Production")
    {
        settings.EnableConsoleLogging = false;
        settings.MaxConcurrentSyncs = 10;
    }

    if (!settings.EnableAutoBackup)
    {
        settings.BackupDirectory = null;
    }

    // Proceed with initialization using the modified settings
    InitializeSyncEngine(settings);
}
```

## Notes

*   **Thread Safety**: The `AppSettings` class contains mutable public properties and a `Dictionary<string, object>`. It is **not thread-safe** by default. If instances are shared across multiple threads (e.g., accessed by both a UI thread and a background sync worker), external synchronization (such as a `lock` statement) is required when reading or writing properties, particularly `SyncProfiles`.
*   **Null Handling**: Properties `LogFilePath` and `BackupDirectory` are nullable (`string?`). Consumers must check for `null` before attempting file I/O operations to avoid `NullReferenceException`.
*   **Validation Dependencies**: Logical consistency between properties is not enforced by the type system alone. For instance, setting `EnableAutoBackup` to `true` while leaving `BackupDirectory` as `null` represents an invalid state that must be caught by the `Validate` logic or external configuration validators.
*   **Dictionary Contents**: The `SyncProfiles` dictionary stores values as `object`. Accessing specific profile configurations requires casting to the expected concrete type; mismatched types will result in `InvalidCastException` at runtime.
*   **Resource Limits**: Properties controlling concurrency (`MaxConcurrentSyncs`) and retries (`MaxRetries`) directly impact system resource usage. Setting these to excessively high values may lead to thread pool exhaustion or API rate-limiting errors.
