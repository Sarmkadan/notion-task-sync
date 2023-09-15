# JsonFormatter

The `JsonFormatter` class provides a centralized utility for serializing and deserializing application-specific data models, such as `Task` and `SyncConfig`, to and from JSON format, while also offering common JSON string manipulation tools including validation, minification, and pretty-printing.

## API

### Constructor
*   `public JsonFormatter()`
    Initializes a new instance of the `JsonFormatter` class.

### Serialization Methods
*   `public string FormatTask(Task task)`
    Serializes a `Task` object into its JSON string representation.
*   `public string FormatTasks(IEnumerable<Task> tasks)`
    Serializes a collection of `Task` objects into a JSON array string.
*   `public string FormatSyncConfig(SyncConfig config)`
    Serializes a `SyncConfig` object into its JSON string representation.
*   `public string Format<T>(T obj)`
    Serializes an object of type `T` into a JSON string.

### Deserialization Methods
*   `public Task? DeserializeTask(string json)`
    Deserializes a JSON string into a `Task` object. Returns `null` if the input string is not a valid representation of a `Task`.
*   `public List<Task>? DeserializeTasks(string json)`
    Deserializes a JSON string into a `List<Task>`. Returns `null` if the input string is not a valid representation of a collection of `Task` objects.
*   `public T? Deserialize<T>(string json)`
    Deserializes a JSON string into an object of type `T`. Returns `null` if deserialization fails.

### Utility Methods
*   `public bool IsValidJson(string json)`
    Validates whether the provided string is syntactically correct JSON.
*   `public string Minify(string json)`
    Removes extraneous whitespace from a JSON string, resulting in a compact format.
*   `public string PrettyPrint(string json)`
    Formats a JSON string by adding indentation and line breaks to improve readability.

## Usage

### Example 1: Serializing a Task
```csharp
var formatter = new JsonFormatter();
var task = new Task { Id = "123", Title = "Update Documentation" };

// Serialize to JSON
string json = formatter.FormatTask(task);
// Result: {"Id":"123","Title":"Update Documentation"}
```

### Example 2: Deserializing and Pretty-Printing Configuration
```csharp
var formatter = new JsonFormatter();
string jsonConfig = "{\"SyncInterval\":300,\"Enabled\":true}";

// Deserialize
var config = formatter.Deserialize<SyncConfig>(jsonConfig);

// Pretty-Print
string formatted = formatter.PrettyPrint(jsonConfig);
/*
Result:
{
  "SyncInterval": 300,
  "Enabled": true
}
*/
```

## Notes

*   **Error Handling:** Serialization methods may throw exceptions if the input object contains circular references or types that cannot be serialized by the underlying JSON provider. Deserialization methods return `null` when input JSON is malformed or incompatible with the target type.
*   **Thread Safety:** The `JsonFormatter` is designed to be stateless and is generally thread-safe, provided that the underlying JSON serializer utilized internally is also configured to be thread-safe.
*   **Performance:** `PrettyPrint` and `Minify` involve parsing and recreating the entire JSON string, which may have performance implications for very large JSON payloads. For high-frequency serialization operations, reusing the same `JsonFormatter` instance is recommended.
