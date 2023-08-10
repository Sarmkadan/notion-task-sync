# SyncConfig

Represents the configuration for a synchronization task between a Notion database and a local folder. It encapsulates all settings required to define the source, destination, behavior, and conflict handling of a sync operation. Instances of `SyncConfig` are typically created, validated, and then passed to a sync engine.

## API

### Properties

- **`Id`** (`Guid`)  
  Unique identifier for this configuration. Automatically assigned on creation.

- **`Name`** (`required string`)  
  A human-readable name for the sync configuration. Must be provided.

- **`NotionDatabaseId`** (`required string`)  
  The identifier of the Notion database to sync with. Must be provided.

- **`LocalFolderPath`** (`required string`)  
  The absolute or relative path to the local folder that will be synchronized. Must be provided.

- **`NotionApiKey`** (`string?`)  
  The Notion API key used for authentication. If not set, the key may be retrieved from a secure store or environment variable.

- **`Direction`** (`SyncDirection`)  
  Determines the direction of synchronization (e.g., Notion to local, local to Notion, bidirectional). Defaults to a value defined by the `SyncDirection` enum.

- **`ConflictStrategy`** (`ConflictResolutionStrategy`)  
  The default strategy to apply when a conflict is detected (e.g., overwrite, skip, ask). Defaults to a value defined by the `ConflictResolutionStrategy` enum.

- **`SyncIntervalSeconds`** (`int`)  
  The interval, in seconds, between automatic syncs. Must be a non-negative integer. Default is typically 300 (5 minutes).

- **`MaxRetries`** (`int`)  
  The maximum number of retry attempts for a failed sync operation. Default is 3.

- **`IsEnabled`** (`bool`)  
  Indicates whether the sync task is active. When `false`, the sync will not run.

- **`CreatedAt`** (`DateTime`)  
  The timestamp when this configuration was created. Set automatically.

- **`UpdatedAt`** (`DateTime`)  
  The timestamp of the last modification to this configuration. Updated automatically on property changes.

- **`LastSyncAt`** (`DateTime?`)  
  The timestamp of the last successful sync execution. `null` if never synced.

- **`NextScheduledSyncAt`** (`DateTime?`)  
  The timestamp of the next scheduled sync. `null` if not scheduled or if `IsEnabled` is `false`.

- **`FieldMappings`** (`Dictionary<string, string>?`)  
  A dictionary mapping local field names (keys) to Notion field names (values). Used to align fields between the two systems. `null` if no custom mapping is defined.

- **`IgnoredFields`** (`List<string>?`)  
  A list of field names that should be excluded from synchronization. `null` if no fields are ignored.

- **`FieldConflictStrategies`** (`Dictionary<string, ConflictResolutionStrategy>?`)  
  Per-field overrides for conflict resolution. Keys are field names; values are the strategy to apply for that field. `null` if no overrides are defined.

### Constructor

- **`SyncConfig()`**  
  Initializes a new instance of `SyncConfig` with default values. Required properties (`Name`, `NotionDatabaseId`, `LocalFolderPath`) must be set after construction, typically via object initializer syntax.

### Methods

- **`bool Validate()`**  
  Validates the configuration against business rules.  
  **Returns:** `true` if the configuration is valid.  
  **Throws:**  
  - `InvalidOperationException` if any required property (`Name`, `NotionDatabaseId`, `LocalFolderPath`) is `null` or empty.  
  - `ArgumentOutOfRangeException` if `SyncIntervalSeconds` is negative.  
  - Additional validation exceptions may be thrown depending on the state of other properties (e.g., invalid field mapping keys).

- **`string? MapLocalFieldToNotion(string localField)`**  
  Resolves the Notion field name corresponding to a given local field name.  
  **Parameters:**  
  - `localField` – The name of the local field to map.  
  **Returns:** The Notion field name if a mapping exists in `FieldMappings`; otherwise `null`.  
  **Throws:** `ArgumentNullException` if `localField` is `null`.

## Usage

### Example 1: Creating and configuring a SyncConfig

```csharp
var config = new SyncConfig
{
    Name = "Tasks Sync",
    NotionDatabaseId = "abc123def456",
    LocalFolderPath = @"C:\Sync\Tasks",
    NotionApiKey = "secret_apikey",
    Direction = SyncDirection.Bidirectional,
    ConflictStrategy = ConflictResolutionStrategy.OverwriteLocal,
    SyncIntervalSeconds = 600,
    MaxRetries = 5,
    IsEnabled = true,
    FieldMappings = new Dictionary<string, string>
    {
        { "Title", "Name" },
        { "DueDate", "Due" }
    },
    IgnoredFields = new List<string> { "InternalNotes" },
    FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
    {
        { "Status", ConflictResolutionStrategy.OverwriteNotion }
    }
};

// Validate before use
if (config.Validate())
{
    Console.WriteLine("Configuration is valid.");
}
```

### Example 2: Using field mapping

```csharp
var config = new SyncConfig
{
    Name = "Simple Sync",
    NotionDatabaseId = "db123",
    LocalFolderPath = "/data/notion"
};

config.FieldMappings = new Dictionary<string, string>
{
    { "local_title", "Title" },
    { "local_due", "Due Date" }
};

// Map a local field to its Notion counterpart
string notionField = config.MapLocalFieldToNotion("local_title");
Console.WriteLine(notionField); // Output: "Title"

// Unmapped field returns null
string unmapped = config.MapLocalFieldToNotion("nonexistent");
Console.WriteLine(unmapped == null ? "No mapping" : unmapped); // Output: "No mapping"
```

## Notes

- **Required properties** (`Name`, `NotionDatabaseId`, `LocalFolderPath`) must be assigned before calling `Validate()` or using the configuration in a sync operation. Failure to do so will cause `Validate()` to throw.
- **Thread safety:** `SyncConfig` is not thread-safe. Concurrent reads and writes to the same instance must be synchronized externally.
- **Default values:** The constructor sets sensible defaults for `SyncIntervalSeconds` (300), `MaxRetries` (3), `Direction`, and `ConflictStrategy`. These can be overridden after construction.
- **Validation** is a one-time check; it does not track subsequent property changes. Re-validate after modifying any property that affects correctness.
- **Field mappings** are case-sensitive. Keys and values are compared using ordinal string comparison.
- **Null collections:** `FieldMappings`, `IgnoredFields`, and `FieldConflictStrategies` are `null` by default. Assign an empty collection if you need to ensure non-null behavior.
- **`MapLocalFieldToNotion`** returns `null` when the local field is not found in `FieldMappings`. It does not throw for missing mappings.
