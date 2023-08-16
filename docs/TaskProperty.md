# TaskProperty

Represents a single property of a task item within the Notion-Task-Sync system. Each `TaskProperty` defines a named attribute with a specific data type, tracks its value, and controls synchronization direction between Notion and the local data store. It also carries metadata about whether the property is required and provides validation and type-safe value retrieval.

## API

### Properties

#### `public Guid Id`
Unique identifier for this property record.

#### `public required Guid TaskId`
The identifier of the parent task to which this property belongs. Must be supplied at construction.

#### `public required string PropertyName`
The name of the property (e.g., "Status", "DueDate", "Priority"). Must be supplied at construction.

#### `public string? PropertyValue`
The current value of the property, stored as a string. Nullable when no value has been assigned.

#### `public PropertyDataType DataType`
Enumerated type indicating the expected data format of this property (e.g., `Text`, `Number`, `Date`, `Select`). Determines how `PropertyValue` is interpreted and validated.

#### `public bool IsRequired`
Indicates whether this property must have a non-null, valid value for the task to be considered complete or valid.

#### `public DateTime CreatedAt`
Timestamp (UTC) when this property record was first created.

#### `public DateTime UpdatedAt`
Timestamp (UTC) when this property record was last modified.

#### `public bool SyncToNotion`
When `true`, changes to this property locally will be pushed to Notion during synchronization.

#### `public bool SyncToLocal`
When `true`, changes to this property in Notion will be pulled to the local store during synchronization.

### Constructors

#### `public TaskProperty()`
Default constructor. Initializes `Id` to a new GUID, sets `CreatedAt` and `UpdatedAt` to the current UTC time, and leaves required fields (`TaskId`, `PropertyName`) uninitialized (must be set via object initializer or subsequent assignment).

### Methods

#### `public bool Validate()`
Validates the current `PropertyValue` against the expected `DataType` and `IsRequired` constraints.
- **Returns**: `true` if the value is valid; `false` otherwise.
- **Behavior**: If `IsRequired` is `true` and `PropertyValue` is null or whitespace, returns `false`. Otherwise, checks that `PropertyValue` can be parsed or interpreted according to `DataType`. Exact validation rules depend on the `PropertyDataType` enumeration implementation.

#### `public T? GetTypedValue<T>()`
Attempts to retrieve the property value converted to the specified type `T`.
- **Type Parameter `T`**: The target type for conversion.
- **Returns**: The converted value of type `T` if conversion succeeds; `null` if `PropertyValue` is null or conversion fails.
- **Throws**: No exceptions are thrown by design; conversion failures silently return `null`.

#### `public bool UpdateValue(string? newValue)`
Updates the `PropertyValue` and refreshes the `UpdatedAt` timestamp.
- **Parameter `newValue`**: The new value to assign. Can be `null`.
- **Returns**: `true` if the update was applied successfully; `false` if the new value fails validation (when validation is performed as part of the update).
- **Side Effects**: Sets `UpdatedAt` to the current UTC time on success.

#### `public override string ToString()`
Returns a string representation of the property, typically combining the `PropertyName` and `PropertyValue` for display or debugging purposes.

## Usage

### Example 1: Creating and Validating a Required Property

```csharp
var dueDateProperty = new TaskProperty
{
    TaskId = task.Id,
    PropertyName = "DueDate",
    DataType = PropertyDataType.Date,
    IsRequired = true,
    SyncToNotion = true,
    SyncToLocal = false
};

// Assign a value and validate
dueDateProperty.UpdateValue("2025-12-31");
if (dueDateProperty.Validate())
{
    Console.WriteLine("Property is valid and ready for sync.");
}
else
{
    Console.WriteLine("Validation failed — check value format.");
}
```

### Example 2: Retrieving Typed Values and Controlling Sync Direction

```csharp
var priorityProperty = new TaskProperty
{
    TaskId = task.Id,
    PropertyName = "Priority",
    DataType = PropertyDataType.Select,
    PropertyValue = "High",
    SyncToNotion = false,
    SyncToLocal = true
};

// Retrieve as enum or string
string? rawValue = priorityProperty.GetTypedValue<string>();
Console.WriteLine($"Priority: {rawValue ?? "not set"}");

// Change sync direction at runtime
priorityProperty.SyncToNotion = true;
priorityProperty.UpdateValue("Low");
```

## Notes

- **Validation Timing**: `Validate()` is an explicit call. `UpdateValue` may internally call validation depending on implementation; if it does and validation fails, the value is not updated and `false` is returned. Always check the return value of `UpdateValue` when data integrity is critical.
- **Null Handling**: `PropertyValue` is nullable by design. `GetTypedValue<T>` returns `null` for both missing values and failed conversions — callers should not rely on it to distinguish between the two cases.
- **Thread Safety**: This class is not inherently thread-safe. Concurrent modifications to `PropertyValue` or sync flags from multiple threads may lead to race conditions. External synchronization (e.g., `lock`) should be used if instances are shared across threads.
- **Sync Flags Independence**: `SyncToNotion` and `SyncToLocal` operate independently. Setting both to `false` effectively excludes the property from all synchronization passes while retaining it locally.
- **Required Fields**: `TaskId` and `PropertyName` are marked `required`. Instantiation must provide these values, either via constructor initialization or object initializer syntax, otherwise a compile-time error will occur (C# 11+ required members).
- **Timestamp Updates**: `UpdatedAt` is only modified on successful calls to `UpdateValue`. Changing sync flags or other metadata directly does not automatically refresh the timestamp.
