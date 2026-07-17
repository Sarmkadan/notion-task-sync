# HealthCheckWorkerValidation

The `HealthCheckWorkerValidation` class provides static utility methods for validating the configuration and state of health check workers within the `notion-task-sync` application. It serves as a centralized guard clause mechanism to ensure that worker dependencies meet required criteria before execution, offering both boolean validation checks and assertion-style methods that throw exceptions upon failure.

## API

### Validate
```csharp
public static IReadOnlyList<string> Validate()
```
Performs a comprehensive validation check on the current health check worker context.
*   **Purpose**: Aggregates all validation errors into a list of descriptive messages.
*   **Parameters**: None.
*   **Return Value**: An `IReadOnlyList<string>` containing error messages. If the worker configuration is valid, the list is empty.
*   **Throws**: This method does not throw exceptions; it returns an empty collection to indicate success.

### IsValid
```csharp
public static bool IsValid()
```
Determines whether the health check worker configuration is currently valid.
*   **Purpose**: Provides a quick boolean flag for conditional logic without retrieving specific error details.
*   **Parameters**: None.
*   **Return Value**: `true` if the configuration passes all validation rules; otherwise, `false`.
*   **Throws**: This method does not throw exceptions.

### EnsureValid
```csharp
public static void EnsureValid()
```
Asserts that the health check worker configuration is valid, halting execution if errors are detected.
*   **Purpose**: Acts as a guard clause to prevent further processing when the worker state is invalid.
*   **Parameters**: None.
*   **Return Value**: `void`. Returns normally only if validation passes.
*   **Throws**: Throws an exception (typically `InvalidOperationException` or a custom validation exception) if the configuration is invalid. The exception message usually aggregates the errors found during validation.

## Usage

### Example 1: Pre-flight Check with Detailed Error Reporting
Use `Validate` when you need to log specific configuration issues or return a structured error response to a caller without terminating the flow immediately.

```csharp
using System;
using System.Linq;

public class HealthCheckRunner
{
    public void RunDiagnostics()
    {
        var errors = HealthCheckWorkerValidation.Validate();

        if (errors.Any())
        {
            Console.WriteLine("Health check worker configuration failed:");
            foreach (var error in errors)
            {
                Console.WriteLine($"- {error}");
            }
            // Proceed with fallback logic or abort gracefully
            return;
        }

        Console.WriteLine("Configuration valid. Starting diagnostics...");
        // Execute worker logic
    }
}
```

### Example 2: Guard Clause in Service Initialization
Use `EnsureValid` at the entry point of a service or background task to fail fast if the environment is not correctly configured.

```csharp
using System;

public class SyncService
{
    public void InitializeWorker()
    {
        // Throws immediately if configuration is invalid, preventing partial initialization
        HealthCheckWorkerValidation.EnsureValid();

        // Safe to proceed with resource allocation
        Console.WriteLine("Initializing health check worker...");
    }
}
```

## Notes

*   **Thread Safety**: As the class exposes only static methods and relies on immutable return types (`IReadOnlyList<string>`, `bool`), it is designed to be thread-safe for concurrent read operations. However, the underlying state being validated must remain consistent during the call.
*   **Redundancy in Signatures**: The API surface lists duplicate signatures for `Validate`, `IsValid`, and `EnsureValid`. In practice, these represent a single implementation per method name. Calls to these methods will resolve to the same logic regardless of overload resolution ambiguity, as no parameters differentiate them.
*   **Empty Collections**: When `Validate` returns an empty `IReadOnlyList<string>`, it strictly indicates a successful validation state. Consumers should check `Count` or use `.Any()` rather than checking for `null`, as the return type guarantees a non-null collection.
*   **Exception Content**: When `EnsureValid` throws, the exception message typically concatenates the strings that would have been returned by `Validate`, providing a single aggregated error string for logging or debugging.
