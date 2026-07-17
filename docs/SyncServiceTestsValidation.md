# SyncServiceTestsValidation

Utility class providing validation methods for testing synchronization services. It offers static methods to validate synchronization state and throw descriptive exceptions when invariants are violated, enabling concise test assertions for service behavior.

## API

### `public static IReadOnlyList<string> Validate`

Returns a list of validation error messages describing any invariants that are violated in the current synchronization state. Each message is a human-readable description of a specific validation failure. Returns an empty list when all invariants are satisfied.

- **Returns**: `IReadOnlyList<string>` — an immutable list of error messages; empty when valid
- **Applies to**: the current state of the synchronization service under test

### `public static bool IsValid`

Determines whether the current synchronization state satisfies all validation invariants. Returns `true` when no validation errors exist, `false` otherwise.

- **Returns**: `bool` — `true` if the state is valid; `false` if any invariant is violated
- **Applies to**: the current state of the synchronization service under test

### `public static void EnsureValid`

Validates the current synchronization state and throws a `SyncValidationException` with a combined message of all validation failures if any invariant is violated. If the state is valid, the method returns normally without throwing.

- **Throws**: `SyncValidationException` — when one or more validation invariants are violated; the exception message contains all error messages concatenated
- **Applies to**: the current state of the synchronization service under test

## Usage

```csharp
// Example 1: Validating state in a unit test
[Fact]
public void SyncService_WhenStateIsValid_ReportsNoErrors()
{
    // Arrange
    var service = new SyncService(/* ... */);
    service.CompleteInitialSync();

    // Act
    var errors = SyncServiceTestsValidation.Validate;

    // Assert
    Assert.Empty(errors);
}

// Example 2: Using EnsureValid to fail fast in integration tests
[Fact]
public void SyncService_WhenStateIsInvalid_ThrowsDescriptiveException()
{
    // Arrange
    var service = new SyncService(/* ... */);
    service.FailWithError();

    // Act & Assert
    var ex = Assert.Throws<SyncValidationException>(
        () => SyncServiceTestsValidation.EnsureValid()
    );
    Assert.Contains("failed to synchronize", ex.Message, StringComparison.OrdinalIgnoreCase);
}
```

## Notes

- All validation methods operate on the current state of the synchronization service instance being tested; they do not accept parameters or operate on external state
- Validation is read-only and does not modify the service state
- The validation logic is deterministic: repeated calls with identical state return identical results
- Thread safety is guaranteed by the caller: these are static methods that read service state; the caller must ensure thread-safe access to the service instance being validated
- Validation messages are intended for test diagnostics and should not be parsed programmatically; their format may change between releases
- When multiple invariants are violated, `Validate` returns all error messages; `EnsureValid` combines them into a single exception message