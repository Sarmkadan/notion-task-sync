# CollectionExtensionsJsonExtensions

Provides JSON serialization and deserialization helpers for the `CollectionExtensionsMarker` type, enabling conversion between marker instances and their JSON representation.

## API
### CollectionExtensionsMarker
A static member that provides a default `CollectionExtensionsMarker` instance.  
- **Return value**: A `CollectionExtensionsMarker` value.  
- **Exceptions**: None.

### Equals
Determines whether the current instance is equal to another object.  
- **Return value**: `true` if the specified object is equal to the current instance; otherwise `false`.  
- **Exceptions**: None.

### Equals (override)
Serves as the overridden equality check for the type.  
- **Return value**: `true` if the specified object is equal to the current instance; otherwise `false`.  
- **Exceptions**: None.

### GetHashCode
Computes a hash code for the current instance.  
- **Return value**: An integer hash code.  
- **Exceptions**: None.

### ToString
Returns a string that represents the current instance.  
- **Return value**: A string representation of the instance.  
- **Exceptions**: None.

### ToJson
Serializes a `CollectionExtensionsMarker` instance to its JSON string representation.  
- **Return value**: A JSON‑encoded string.  
- **Exceptions**: May throw `ArgumentNullException` if the input instance is `null`.

### FromJson
Deserializes a JSON string to a `CollectionExtensionsMarker` instance.  
- **Return value**: A `CollectionExtensionsMarker` value, or `null` if the input is `null` or invalid.  
- **Exceptions**: May throw `JsonException` if the JSON is malformed.

### TryFromJson
Attempts to deserialize a JSON string to a `CollectionExtensionsMarker` instance.  
- **Return value**: `true` if deserialization succeeded; otherwise `false`.  
- **Exceptions**: None. On failure the output variable is set to `null`.

## Usage
```csharp
using NotionTaskSync.Json;

// Serializing a marker to JSON
CollectionExtensionsMarker marker = CollectionExtensionsJsonExtensions.CollectionExtensionsMarker;
string json = CollectionExtensionsJsonExtensions.ToJson(marker);
// json can now be stored, transmitted, or cached
```

```csharp
using NotionTaskSync.Json;

// Deserializing JSON safely
string json = GetJsonFromSomewhere(); // assume this returns a JSON string
if (CollectionExtensionsJsonExtensions.TryFromJson(json, out CollectionExtensionsMarker? marker))
{
    // marker contains the deserialized value
    ProcessMarker(marker);
}
else
{
    // handle invalid or missing JSON
    Log.Warning("Failed to parse CollectionExtensionsMarker JSON.");
}
```

## Notes
- The static JSON methods do not retain any internal state; they are thread‑safe and may be called concurrently from multiple threads.  
- `Equals` and `GetHashCode` rely only on the instance’s data and therefore are also thread‑safe for read‑only access.  
- `FromJson` will throw when the supplied JSON cannot be parsed; callers expecting invalid data should prefer `TryFromJson`, which returns `false` instead of throwing.  
- Passing `null` to `ToJson` results in an `ArgumentNullException`; `FromJson` accepts `null` and returns `null` without throwing.  
- The `CollectionExtensionsMarker` field is intended as a sentinel or default value; it is immutable and safe for shared use.
