# ValidationHelper

The `ValidationHelper` class provides a centralized collection of static utility methods designed to validate input data formats and constraints specific to the `notion-task-sync` project. It ensures data integrity for Notion API interactions, file system operations, and general configuration values by enforcing strict rules for identifiers, paths, credentials, and numeric ranges without maintaining internal state.

## API

### `IsValidNotionId`
Determines whether a given string conforms to the format of a valid Notion Object ID.
- **Parameters**: `string id` – The string to validate.
- **Returns**: `bool` – `true` if the string matches the expected Notion ID pattern (typically a UUID or legacy formatted ID); otherwise, `false`.
- **Throws**: No exceptions are thrown; invalid inputs return `false`.

### `IsValidEmail`
Validates whether a string represents a syntactically correct email address.
- **Parameters**: `string email` – The email address string to check.
- **Returns**: `bool` – `true` if the format is valid; otherwise, `false`.
- **Throws**: No exceptions are thrown; null or empty strings return `false`.

### `IsValidFilePath`
Checks if a string represents a valid, well-formed file path according to the current operating system's rules.
- **Parameters**: `string path` – The file path string to validate.
- **Returns**: `bool` – `true` if the path syntax is valid; otherwise, `false`.
- **Throws**: No exceptions are thrown. This method checks syntax only, not existence.

### `IsValidDirectoryPath`
Checks if a string represents a valid, well-formed directory path.
- **Parameters**: `string path` – The directory path string to validate.
- **Returns**: `bool` – `true` if the path syntax is valid for a directory; otherwise, `false`.
- **Throws**: No exceptions are thrown. This method checks syntax only, not existence.

### `IsValidApiKey`
Validates the format of a Notion API integration key.
- **Parameters**: `string apiKey` – The API key string to validate.
- **Returns**: `bool` – `true` if the key matches the expected secret format (e.g., specific prefix and length); otherwise, `false`.
- **Throws**: No exceptions are thrown.

### `IsValidPriority`
Determines if a given value represents a supported task priority level.
- **Parameters**: `int priority` (or equivalent enum representation) – The priority value to check.
- **Returns**: `bool` – `true` if the value corresponds to a defined priority (e.g., Low, Medium, High); otherwise, `false`.
- **Throws**: No exceptions are thrown.

### `IsInRange`
Checks whether a numeric value falls within a specified inclusive range.
- **Parameters**: `int value`, `int min`, `int max` – The value to check and the boundary limits.
- **Returns**: `bool` – `true` if `min <= value <= max`; otherwise, `false`.
- **Throws**: No exceptions are thrown.

### `IsLengthValid`
Validates whether a string's length falls within a specified range.
- **Parameters**: `string input`, `int minLength`, `int maxLength` – The string to check and the allowed length boundaries.
- **Returns**: `bool` – `true` if the string length is within the range; otherwise, `false`.
- **Throws**: No exceptions are thrown; null inputs typically return `false`.

### `SanitizeString`
Cleans an input string by removing or escaping invalid characters for safe processing or storage.
- **Parameters**: `string input` – The raw string to sanitize.
- **Returns**: `string` – The sanitized version of the input. Returns an empty string if input is null.
- **Throws**: No exceptions are thrown.

### `IsValidIdentifierName`
Checks if a string is a valid identifier name suitable for use in code generation or internal referencing.
- **Parameters**: `string name` – The proposed identifier name.
- **Returns**: `bool` – `true` if the name adheres to standard identifier rules (starts with letter/underscore, contains only alphanumeric/underscores); otherwise, `false`.
- **Throws**: No exceptions are thrown.

### `IsValidUrl`
Validates whether a string is a well-formed absolute URL.
- **Parameters**: `string url` – The URL string to validate.
- **Returns**: `bool` – `true` if the string is a valid absolute URI with a recognized scheme (http/https); otherwise, `false`.
- **Throws**: No exceptions are thrown.

## Usage

The following examples demonstrate how to use `ValidationHelper` to enforce data constraints before executing critical operations such as API calls or file I/O.

```csharp
// Example 1: Validating Notion API credentials and target ID before synchronization
public async Task SyncTaskAsync(string apiKey, string notionId)
{
    if (!ValidationHelper.IsValidApiKey(apiKey))
    {
        throw new ArgumentException("Invalid Notion API Key format.", nameof(apiKey));
    }

    if (!ValidationHelper.IsValidNotionId(notionId))
    {
        throw new ArgumentException("Target Notion ID is malformed.", nameof(notionId));
    }

    // Proceed with API call using validated inputs
    await _notionClient.UpdateBlockAsync(apiKey, notionId);
}
```

```csharp
// Example 2: Validating configuration paths and priority settings for local caching
public void ConfigureCache(string cachePath, int priorityLevel)
{
    if (!ValidationHelper.IsValidDirectoryPath(cachePath))
    {
        Console.WriteLine("Error: The specified cache directory path is invalid.");
        return;
    }

    if (!ValidationHelper.IsValidPriority(priorityLevel))
    {
        Console.WriteLine("Error: Priority level must be between 1 and 5.");
        return;
    }

    if (!ValidationHelper.IsLengthValid(cachePath, 1, 260))
    {
        Console.WriteLine("Error: Cache path exceeds maximum allowed length.");
        return;
    }

    _cacheService.Initialize(cachePath, priorityLevel);
}
```

## Notes

- **Thread Safety**: All methods in `ValidationHelper` are static and stateless, relying solely on input parameters. Consequently, the class is inherently thread-safe and can be called concurrently from multiple threads without synchronization.
- **Null Handling**: Boolean validation methods generally return `false` when passed `null` rather than throwing `ArgumentNullException`, allowing for concise guard clauses. The `SanitizeString` method returns an empty string for `null` inputs.
- **Path Validation**: `IsValidFilePath` and `IsValidDirectoryPath` perform syntactic validation based on the current runtime environment (Windows vs. Unix-like systems). They do not verify if the path actually exists on the disk.
- **Edge Cases**: 
  - `IsValidUrl` requires an absolute URL with a scheme; relative URLs will return `false`.
  - `IsValidIdentifierName` follows C# language specification rules for identifiers, which may differ from Notion property naming conventions.
  - `SanitizeString` behavior regarding specific character sets (e.g., control characters vs. special symbols) is implementation-dependent but guarantees the output is safe for standard string operations.
