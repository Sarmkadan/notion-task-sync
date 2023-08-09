# ConfigureCommand

A command class used to configure synchronization settings between Notion and a local task directory. It collects user-provided configuration values for API access, database targeting, synchronization behavior, and conflict resolution, then applies them to the application’s configuration system.

## API

### `ConfigureCommand`
Initializes a new instance of the `ConfigureCommand` class with default or empty values for all configuration fields.

### `public override async Task<int> ExecuteAsync()`
Executes the configuration command, applying the provided settings to the application’s configuration store and returning an exit code indicating success or failure.

- **Returns**
  `Task<int>`: A task that resolves to an integer exit code. A return value of `0` indicates success; non-zero values indicate specific error conditions.

- **Throws**
  `InvalidOperationException`: When required configuration values are missing or invalid during execution.
  `ConfigurationException`: When the application’s configuration system fails to persist the provided settings.

### `public string? ApiKey`
Gets or sets the Notion API key used to authenticate requests to the Notion API.

- **Type**: `string?`
- **Purpose**: Specifies the API key required to access the Notion database and perform synchronization operations.
- **Constraints**: Must be a valid Notion integration token. If `null` or empty during execution, the command will throw an exception.

### `public string? DatabaseId`
Gets or sets the Notion database ID that serves as the target for task synchronization.

- **Type**: `string?`
- **Purpose**: Identifies the specific Notion database where tasks will be read from and written to.
- **Constraints**: Must be a valid Notion database ID. If `null` or empty during execution, the command will throw an exception.

### `public string TaskDirectory`
Gets or sets the local directory path where task data is stored and synchronized from.

- **Type**: `string`
- **Purpose**: Defines the filesystem location used to read and write task data during synchronization.
- **Constraints**: Must be a valid, accessible directory path. If the path does not exist or is not writable, the command will throw an exception.

### `public int SyncIntervalSeconds`
Gets or sets the synchronization interval in seconds between local task updates and Notion.

- **Type**: `int`
- **Purpose**: Controls how frequently background synchronization processes are triggered.
- **Constraints**: Must be a positive integer. Values less than `1` are treated as invalid and will cause the command to throw during execution.

### `public string ConflictStrategy`
Gets or sets the strategy used to resolve conflicts when local and remote task versions differ.

- **Type**: `string`
- **Purpose**: Determines the behavior when synchronization detects conflicting changes. Expected values include `"local"`, `"remote"`, or `"ask"`.
- **Constraints**: Must be one of the supported conflict resolution strategies. Invalid values will cause the command to throw during execution.

## Usage
