# SyncException

The `SyncException` serves as the base exception type for synchronization failures within the `notion-task-sync` project, encapsulating contextual metadata such as the configuration identifier, occurrence timestamp, and detailed error information. It acts as the parent class for specialized synchronization errors including `NotionApiException`, `LocalFileException`, `ValidationException`, and `ConflictException`, allowing consumers to catch general sync failures or handle specific subsystem errors through inheritance while maintaining a consistent schema for logging and diagnostics.

## API

### Constructors

#### `public SyncException(string message)`
Initializes a new instance of the `SyncException` class with a specified error message.
*   **Parameters**:
    *   `message` (`string`): A human-readable description of the error condition.
*   **Remarks**: Sets the `Message` property of the base `Exception` class. The `OccurredAt` property is automatically set to the current UTC time upon instantiation.

#### `public NotionApiException(string message)`
Initializes a new instance of the `NotionApiException` class, representing errors returned by the Notion API.
*   **Parameters**:
    *   `message` (`string`): A description of the API error.
*   **Remarks**: Inherits from `SyncException`. Used when HTTP requests to Notion fail or return error payloads.

#### `public LocalFileException(string message)`
Initializes a new instance of the `LocalFileException` class, representing errors related to local file system operations during sync.
*   **Parameters**:
    *   `message` (`string`): A description of the file system error.
*   **Remarks**: Inherits from `SyncException`. Used when reading, writing, or accessing local cache files fails.

#### `public ValidationException(string message)`
Initializes a new instance of the `ValidationException` class, representing data validation failures before or during sync.
*   **Parameters**:
    *   `message` (`string`): A description of the validation failure.
*   **Remarks**: Inherits from `SyncException`. Used when input data does not meet schema requirements.

#### `public ConflictException(string message)`
Initializes a new instance of the `ConflictException` class, representing concurrency conflicts between local and remote states.
*   **Parameters**:
    *   `message` (`string`): A description of the conflict.
*   **Remarks**: Inherits from `SyncException`. Used when version mismatches or duplicate keys prevent synchronization.

### Properties

#### `public string? SyncConfigId`
Gets the identifier of the synchronization configuration associated with the error.
*   **Type**: `string?`
*   **Purpose**: Allows filtering logs or alerts by specific sync jobs. Null if the error occurred before a configuration context was established.

#### `public DateTime OccurredAt`
Gets the precise date and time when the exception was instantiated.
*   **Type**: `DateTime`
*   **Purpose**: Provides an immutable timestamp for auditing and correlating events across distributed logs.

#### `public string? Details`
Gets additional contextual information or stack traces relevant to the sync failure.
*   **Type**: `string?`
*   **Purpose**: Contains extended debugging information that may be too verbose for the standard `Message` property.

#### `public int? HttpStatusCode`
Gets the HTTP status code returned by the Notion API.
*   **Type**: `int?`
*   **Scope**: `NotionApiException` only.
*   **Purpose**: Distinguishes between client errors (4xx) and server errors (5xx). Null for non-API exceptions.

#### `public string? ApiErrorCode`
Gets the specific error code string returned by the Notion API.
*   **Type**: `string?`
*   **Scope**: `NotionApiException` only.
*   **Purpose**: Provides programmatic identification of known API error types (e.g., "rate_limited", "object_not_found").

#### `public string? FilePath`
Gets the path to the local file involved in the operation.
*   **Type**: `string?`
*   **Scope**: `LocalFileException` only.
*   **Purpose**: Identifies the specific file causing the I/O failure.

#### `public string? FieldName`
Gets the name of the data field that failed validation.
*   **Type**: `string?`
*   **Scope**: `ValidationException` only.
*   **Purpose**: Pinpoints the exact schema violation.

#### `public object? InvalidValue`
Gets the actual value that caused the validation failure.
*   **Type**: `object?`
*   **Scope**: `ValidationException` only.
*   **Purpose**: Assists in debugging by showing the problematic data payload.

#### `public Guid? TaskId`
Gets the unique identifier of the task involved in the conflict.
*   **Type**: `Guid?`
*   **Scope**: `ConflictException` only.
*   **Purpose**: Identifies the specific task record causing the concurrency issue.

### Static Methods

#### `public static SyncException CreateWithContext`
Factory method for creating a `SyncException` (or derived type) with pre-populated context metadata.
*   **Parameters**: Contextual arguments required to populate `SyncConfigId`, `Details`, and the specific exception type. (Exact signature parameters inferred from usage patterns: typically accepts message, config ID, and optional inner exception).
*   **Return Value**: An instance of `SyncException` or one of its derived types with `OccurredAt` set and context properties populated.
*   **Purpose**: Ensures consistent error reporting standards across the application without requiring manual property assignment after construction.

## Usage

### Example 1: Handling Specific Subsystem Errors
This example demonstrates catching specific derived exceptions to handle retry logic for API rate limits versus aborting on file system errors.

```csharp
try
{
    await syncService.ExecuteAsync(configId);
}
catch (NotionApiException ex) when (ex.HttpStatusCode == 429)
{
    // Handle rate limiting with exponential backoff
    logger.LogWarning("Rate limited by Notion API. Code: {Code}", ex.ApiErrorCode);
    await Task.Delay(TimeSpan.FromSeconds(5));
    await syncService.RetryAsync(configId);
}
catch (LocalFileException ex)
{
    // Abort sync if local storage is inaccessible
    logger.LogError(ex, "Sync failed due to local file error at {Path}", ex.FilePath);
    throw; // Re-throw to stop the pipeline
}
catch (SyncException ex)
{
    // Generic fallback for other sync issues
    logger.Error(ex, "Sync failed for config {ConfigId} at {Time}", ex.SyncConfigId, ex.OccurredAt);
}
```

### Example 2: Validating Input Data
This example shows how `ValidationException` provides detailed feedback on invalid data fields during the pre-sync phase.

```csharp
public void PrepareTask(TaskDto task)
{
    if (string.IsNullOrWhiteSpace(task.Title))
    {
        throw new ValidationException("Task title cannot be empty")
        {
            FieldName = nameof(task.Title),
            InvalidValue = task.Title,
            SyncConfigId = currentConfig.Id
        };
    }

    if (task.DueDate < DateTime.UtcNow)
    {
        throw new ValidationException("Due date cannot be in the past")
        {
            FieldName = nameof(task.DueDate),
            InvalidValue = task.DueDate,
            Details = "Validation rule: DueDate >= UtcNow"
        };
    }
}
```

## Notes

*   **Thread Safety**: The `SyncException` class and its derived types are immutable after construction (excluding the base `Exception` data), making them safe to share across threads. The `OccurredAt` property is set during construction, ensuring thread-safe timestamping without external synchronization.
*   **Nullability**: Consumers must check for `null` on optional properties (`SyncConfigId`, `Details`, `HttpStatusCode`, etc.) as these are only populated when the error context is available. For instance, `HttpStatusCode` will be null if a `NotionApiException` is thrown due to a network timeout before an HTTP response is received.
*   **Inheritance Hierarchy**: When catching exceptions, order matters. Catch specific derived types (`NotionApiException`, `ConflictException`, etc.) before catching the base `SyncException` to access specialized properties like `TaskId` or `ApiErrorCode`.
*   **Serialization**: As these exceptions may cross process boundaries in distributed logging scenarios, ensure that any custom serialization logic respects the nullable reference types defined in the signature.
