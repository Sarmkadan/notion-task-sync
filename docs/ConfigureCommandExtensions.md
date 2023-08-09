# ConfigureCommandExtensions

Provides static accessors for configuration values used by the `notion-task-sync` command line tool. These members expose the result of configuration file parsing and validation, allowing other parts of the application to retrieve the Notion API key, target database ID, sync interval, and conflict‑resolution strategy without directly accessing the configuration file.

## API

### ValidateConfigurationFile
- **Purpose**: Indicates whether the configuration file has been successfully loaded and passes basic validation.
- **Parameters**: None.
- **Return value**: `true` if the configuration file exists, is well‑formed, and contains the required settings; otherwise `false`.
- **Throws**: None. The value is set during static initialization; if an error occurs while reading the file, the field is set to `false` rather than throwing.

### GetApiKey
- **Purpose**: Retrieves the Notion API key from the configuration.
- **Parameters**: None.
- **Return value**: The API key as a `string` if present; `null` if the key is missing or the configuration file failed validation.
- **Throws**: None. Returns `null` when the key cannot be obtained.

### GetDatabaseId
- **Purpose**: Retrieves the identifier of the Notion database to synchronize with.
- **Parameters**: None.
- **Return value**: The database ID as a `string` if present; `null` if missing or configuration invalid.
- **Throws**: None. Returns `null` when the ID cannot be obtained.

### GetSyncIntervalSeconds
- **Purpose**: Retrieves the configured sync interval in seconds.
- **Parameters**: None.
- **Return value**: The interval as an `int`. If the configuration file is missing or invalid, returns a default value of `0`.
- **Throws**: None. Invalid numeric values are treated as `0`.

### GetConflictStrategy
- **Purpose**: Retrieves the conflict‑resolution strategy to use when local and remote tasks diverge.
- **Parameters**: None.
- **Return value**: A string naming the strategy (e.g., `"local-wins"`, `"remote-wins"`, `"manual"`). Returns an empty string if not specified or configuration invalid.
- **Throws**: None. Missing or unrecognized values yield an empty string.

## Usage

```csharp
using NotionTaskSync.Configuration;

// Check that the configuration is usable before proceeding.
if (!ConfigureCommandExtensions.ValidateConfigurationFile)
{
    Console.Error.WriteLine("Configuration file is missing or invalid.");
    Environment.Exit(1);
}

// Retrieve settings for initializing a Notion client.
string apiKey = ConfigureCommandExtensions.GetApiKey ?? 
                throw new InvalidOperationException("API key not configured.");
string databaseId = ConfigureCommandExtensions.GetDatabaseId ?? 
                    throw new InvalidOperationException("Database ID not configured.");
int interval = ConfigureCommandExtensions.GetSyncIntervalSeconds;
if (interval <= 0)
{
    interval = 300; // fallback to 5 minutes
}
string strategy = ConfigureCommandExtensions.GetConflictStrategy;
if (string.IsNullOrEmpty(strategy))
{
    strategy = "local-wins"; // default strategy
}

// Use the values … 
var notionClient = new NotionClient(apiKey);
var syncService = new SyncService(notionClient, databaseId, TimeSpan.FromSeconds(interval), strategy);
```

```csharp
// Example: logging the effective configuration at startup.
Console.WriteLine("Notion Task Sync configuration:");
Console.WriteLine($"  API Key present: {ConfigureCommandExtensions.GetApiKey != null}");
Console.WriteLine($"  Database ID: {ConfigureCommandExtensions.GetDatabaseId ?? "<not set>"}");
Console.WriteLine($"  Sync interval (s): {ConfigureCommandExtensions.GetSyncIntervalSeconds}");
Console.WriteLine($"  Conflict strategy: {ConfigureCommandExtensions.GetConflictStrategy ?? "<default>"}");
```

## Notes

- The static fields are populated once during type initialization; after that they are immutable for the lifetime of the app domain, making them thread‑safe for concurrent reads.
- If the configuration file cannot be found or is malformed, `ValidateConfigurationFile` will be `false` and the getter methods will return their fallback values (`null` for reference types, `0` for the interval, empty string for the strategy). Callers should check `ValidateConfigurationFile` or explicitly handle fallback values to avoid using unintentionally default settings.
- The `GetSyncIntervalSeconds` method does not validate that the interval is positive; a non‑positive result should be treated as invalid and replaced with a sensible default, as shown in the usage examples.
- The conflict‑strategy string is case‑sensitive in the consuming code; callers may wish to normalize it (e.g., `.ToLowerInvariant()`) before comparison.
