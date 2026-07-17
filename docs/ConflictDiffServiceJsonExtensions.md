# ConflictDiffServiceJsonExtensions

Provides JSON serialization and conversion helpers for the `ConflictDiffService` type, enabling direct conversion to and from JSON strings as well as low‑level read/write operations for related value types (`DiffLineKind`, `DiffLine`, `ConflictDiffResult`, `Guid`, and `DateTime`) used within the synchronization logic.

## API

### ToJson
```csharp
public static string ToJson(ConflictDiffService value)
```
Serializes a `ConflictDiffService` instance to its JSON representation.  
- **Parameters**  
  - `value`: The object to serialize. Passing `null` results in `null` being returned.  
- **Return value**  
  - A JSON‑encoded string representing `value`, or `null` if `value` is `null`.  
- **Exceptions**  
  - Throws `ArgumentException` if the object contains members that cannot be serialized (e.g., circular references).  
  - Throws `InvalidOperationException` if an internal serialization error occurs.

### FromJson
```csharp
public static ConflictDiffService? FromJson(string json)
```
Deserializes a JSON string into a `ConflictDiffService` instance.  
- **Parameters**  
  - `json`: The JSON input. If `null` or empty, the method returns `null`.  
- **Return value**  
  - The deserialized `ConflictDiffService` object, or `null` when `json` is `null`, empty, or does not represent a valid instance.  
- **Exceptions**  
  - Throws `JsonException` if `json` is malformed or does not match the expected schema.

### TryFromJson
```csharp
public static bool TryFromJson(string json, out ConflictDiffService? result)
```
Attempts to parse a JSON string into a `ConflictDiffService` without throwing on failure.  
- **Parameters**  
  - `json`: The JSON input. May be `null` or empty.  
  - `result`: Receives the deserialized object when the method returns `true`; otherwise receives `null`.  
- **Return value**  
  - `true` if `json` was successfully parsed; otherwise `false`.  
- **Exceptions**  
  - Does not throw for parsing errors; any unexpected internal error (e.g., out‑of‑memory) may still propagate.

### Read (DiffLineKind)
```csharp
public override DiffLineKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```
Reads a `DiffLineKind` enum value from the JSON stream.  
- **Parameters**  
  - `reader`: The UTF‑8 JSON reader positioned at the token to read.  
  - `typeToConvert`: The type being converted (ignored for this overload).  
  - `options`: Serialization options (ignored).  
- **Return value**  
  - The `DiffLineKind` value represented by the current JSON token.  
- **Exceptions**  
  - Throws `JsonException` if the token cannot be interpreted as a valid `DiffLineKind`.

### Write (DiffLineKind)
```csharp
public override void Write(Utf8JsonWriter writer, DiffLineKind value, JsonSerializerOptions options)
```
Writes a `DiffLineKind` enum value as a JSON token.  
- **Parameters**  
  - `writer`: The UTF‑8 JSON writer to which the value is written.  
  - `value`: The `DiffLineKind` to serialize.  
  - `options`: Serialization options (ignored).  
- **Return value**  
  - None.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `writer` is `null`.  
  - Throws `InvalidOperationException` if the writer is in an invalid state.

### Read (DiffLine)
```csharp
public override DiffLine Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```
Deserializes a `DiffLine` object from the JSON stream.  
- **Parameters**  
  - `reader`: Positioned at the start of the `DiffLine` token.  
  - `typeToConvert`: The target type (`DiffLine`).  
  - `options`: Serialization options.  
- **Return value**  
  - A new `DiffLine` instance populated from the JSON.  
- **Exceptions**  
  - Throws `JsonException` if the JSON does not conform to the expected `DiffLine` structure.  
  - Throws `NotSupportedException` if a property type cannot be resolved.

### Write (DiffLine)
```csharp
public override void Write(Utf8JsonWriter writer, DiffLine value, JsonSerializerOptions options)
```
Serializes a `DiffLine` instance to JSON.  
- **Parameters**  
  - `writer`: Destination UTF‑8 JSON writer.  
  - `value`: The `DiffLine` to serialize.  
  - `options`: Serialization options.  
- **Return value**  
  - None.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `writer` or `value` is `null`.  
  - Throws `InvalidOperationException` if serialization fails.

### Read (ConflictDiffResult)
```csharp
public override ConflictDiffResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```
Reads a `ConflictDiffResult` value from the JSON stream.  
- **Parameters**  
  - `reader`: Positioned at the token to read.  
  - `typeToConvert`: The type being converted (`ConflictDiffResult`).  
  - `options`: Serialization options.  
- **Return value**  
  - The deserialized `ConflictDiffResult`.  
- **Exceptions**  
  - Throws `JsonException` for malformed JSON or missing required properties.  
  - Throws `NotSupportedException` if an enum value is undefined.

### Write (ConflictDiffResult)
```csharp
public override void Write(Utf8JsonWriter writer, ConflictDiffResult value, JsonSerializerOptions options)
```
Writes a `ConflictDiffResult` instance as JSON.  
- **Parameters**  
  - `writer`: Destination UTF‑8 JSON writer.  
  - `value`: The `ConflictDiffResult` to serialize.  
  - `options`: Serialization options.  
- **Return value**  
  - None.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `writer` or `value` is `null`.  
  - Throws `InvalidOperationException` on serialization failure.

### GetGuid
```csharp
public static Guid GetGuid(JsonElement element)
```
Extracts a `Guid` from a `System.Text.Json.JsonElement`.  
- **Parameters**  
  - `element`: The JSON element containing a string representation of a GUID.  
- **Return value**  
  - The parsed `Guid`.  
- **Exceptions**  
  - Throws `FormatException` if the element’s value is not a valid GUID string.  
  - Throws `InvalidOperationException` if the element’s value kind is not `String`.

### GetDateTime
```csharp
public static DateTime GetDateTime(JsonElement element)
```
Extracts a `DateTime` from a `System.Text.Json.JsonElement`.  
- **Parameters**  
  - `element`: The JSON element containing a string representation of a date/time.  
- **Return value**  
  - The parsed `DateTime` (Kind is `Utc` if the string includes a timezone offset, otherwise `Unspecified`).  
- **Exceptions**  
  - Throws `FormatException` if the string is not a recognizable date/time format.  
  - Throws `InvalidOperationException` if the element’s value kind is not `String`.

## Usage

### Serializing and deserializing a ConflictDiffService
```csharp
using System.Text.Json;
using NotionTaskSync.Services; // namespace containing ConflictDiffService

var service = new ConflictDiffService { /* initialize properties */ };

// Convert to JSON
string json = ConflictDiffServiceJsonExtensions.ToJson(service);

// Convert back from JSON
ConflictDiffService? restored = ConflictDiffServiceJsonExtensions.FromJson(json);
if (restored == null)
{
    // Handle deserialization failure
}
```

### Using the low‑level converters within a custom JsonConverter
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public class ConflictDiffServiceConverter : JsonConverter<ConflictDiffService>
{
    public override ConflictDiffService Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Assume the JSON is an object with a "guid" and a "timestamp" field
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        Guid guid = ConflictDiffServiceJsonExtensions.GetGuid(root.GetProperty("guid"));
        DateTime timestamp = ConflictDiffServiceJsonExtensions.GetDateTime(root.GetProperty("timestamp"));

        return new ConflictDiffService { Id = guid, LastModified = timestamp };
    }

    public override void Write(Utf8JsonWriter writer, ConflictDiffService value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("guid", value.Id.ToString());
        writer.WriteString("timestamp", value.LastModified.ToString("o"));
        writer.WriteEndObject();
    }
}
```

## Notes

- All static extension methods (`ToJson`, `FromJson`, `TryFromJson`, `GetGuid`, `GetDateTime`) are stateless and thread‑safe; they may be invoked concurrently from multiple threads without external synchronization.  
- The override `Read`/`Write` members are intended for use by `System.Text.Json.Serialization.JsonConverter<T>` implementations; they assume the supplied `Utf8JsonReader`/`Utf8JsonWriter` is correctly positioned and not `null`. Passing a `null` writer will result in an `ArgumentNullException`.  
- `FromJson` returns `null` for empty or `null` input, whereas `TryFromJson` treats the same inputs as a failure and returns `false`. Callers should choose the API that matches their error‑handling preference.  
- DateTime parsing follows the round‑trip ("o") format; strings that lack timezone information are parsed as `Unspecified` Kind. Applications requiring a specific Kind should adjust the returned value accordingly.  
- Guid parsing expects the canonical hyphenated format (e.g., `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`). Any deviation triggers a `FormatException`.  
- The converters do not perform culture‑specific parsing; they rely on the invariant formats emitted by the corresponding `Write` methods. Changing serialization options that affect string formatting (e.g., custom `DateTimeFormatInfo`) may break deserialization unless the same options are applied consistently.  
- Instances of `ConflictDiffService` containing circular references or unsupported property types may cause `ToJson`/`Write` to throw; ensure the object graph is acyclic and composed of JSON‑serializable types before invoking these members.
