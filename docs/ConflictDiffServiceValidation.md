# ConflictDiffServiceValidation

The `ConflictDiffServiceValidation` class provides static validation utilities for inputs consumed by the conflict-diff service within the `notion-task-sync` project. It centralizes checks for correctness and completeness of configuration data, task identifiers, and other parameters before they are passed to the service. The class exposes multiple overloads of a `Validate` method, a boolean `IsValid` property, and an `EnsureValid` method to support both diagnostic and defensive validation patterns.

## API

### `Validate` (5 overloads)

```csharp
public static IReadOnlyList<string> Validate(/* input */)
```

There are five overloads of the `Validate` method, each accepting a different type of input (e.g., a configuration object, a string identifier, a collection of parameters, etc.). All overloads perform the same logical validation for their respective input type.

- **Purpose**: Validates the specified input against the service’s constraints.
- **Parameters**: The input to validate. The exact type varies per overload.
- **Returns**: An `IReadOnlyList<string>` containing human-readable error messages. If the input is valid, the returned list is empty (count = 0).
- **Throws**: None. The method never throws; all validation failures are reported via the returned list.

### `IsValid`

```csharp
public static bool IsValid { get; }
```

- **Purpose**: Gets a value indicating whether the most recent validation call (via `Validate` or `EnsureValid`) succeeded.
- **Value**: `true` if the last validation returned no errors; otherwise `false`. The initial value before any validation call is `false`.
- **Throws**: Never.

### `EnsureValid`

```csharp
public static void EnsureValid(/* input */)
```

- **Purpose**: Validates the specified input and throws an exception if any validation errors are found.
- **Parameters**: The input to validate. The exact type matches one of the `Validate` overloads.
- **Returns**: Nothing.
- **Throws**: An `InvalidOperationException` (or a more specific derived exception) containing a combined message of all validation errors, if the input is invalid. If the input is valid, the method completes normally.

## Usage

### Example 1: Collecting validation errors without throwing

```csharp
using System;
using NotionTaskSync.ConflictDiff;

public class ConfigLoader
{
    public bool TryLoadConfig(string configJson)
    {
        var errors = ConflictDiffServiceValidation.Validate(configJson);
        if (errors.Count > 0)
        {
            Console.WriteLine("Configuration is invalid:");
            foreach (var error in errors)
                Console.WriteLine($"  - {error}");
            return false;
        }
        return true;
    }
}
```

### Example 2: Defensive validation with EnsureValid

```csharp
using System;
using NotionTaskSync.ConflictDiff;

public class TaskProcessor
{
    public void ProcessTask(string taskId)
    {
        // Throws if taskId is null, empty, or malformed
        ConflictDiffServiceValidation.EnsureValid(taskId);

        // Safe to proceed
        Console.WriteLine($"Processing task {taskId}...");
    }
}
```

## Notes

- **Null and empty inputs**: All `Validate` overloads treat `null` and empty strings as invalid and return appropriate error messages. `EnsureValid` throws for these cases.
- **Error message format**: Error messages are plain English strings intended for logging or user display. They are not localized.
- **Thread safety**: The `Validate` and `EnsureValid` methods are stateless and thread-safe; they do not modify any shared state. The `IsValid` property, however, reflects the outcome of the most recent validation call on the current thread. If multiple threads call validation methods concurrently, the value of `IsValid` is undefined and should not be relied upon for cross-thread decisions. For thread-safe validation, always use the return value of `Validate` or the exception from `EnsureValid` directly.
- **Overload resolution**: When calling `Validate` or `EnsureValid`, the compiler selects the appropriate overload based on the argument type. Ensure the argument type matches one of the supported overloads exactly to avoid ambiguity.
