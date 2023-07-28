# StringExtensionsTestsExtensions

The `StringExtensionsTestsExtensions` class provides a centralized set of static test execution routines for validating the behavior of string manipulation utilities within the `notion-task-sync` project. These methods facilitate comprehensive verification of extension methods by encapsulating relevant test cases and assertions into reusable test runner procedures.

## API

### RunTruncateTest
Executes the validation suite for string truncation functionality.
*   **Parameters:** None.
*   **Return Value:** `void`.
*   **Throws:** Throws an exception if any assertion within the truncation test suite fails.

### RunSanitizeTests
Executes the validation suite for string sanitization functionality, covering common edge cases for input cleaning.
*   **Parameters:** None.
*   **Return Value:** `void`.
*   **Throws:** Throws an exception if any assertion within the sanitization test suite fails.

### RunToSnakeCaseTest
Executes the validation suite for converting strings to `snake_case` format.
*   **Parameters:** None.
*   **Return Value:** `void`.
*   **Throws:** Throws an exception if any assertion within the snake_case conversion test suite fails.

### RunToSlugTest
Executes the validation suite for URL-friendly slug generation.
*   **Parameters:** None.
*   **Return Value:** `void`.
*   **Throws:** Throws an exception if any assertion within the slug generation test suite fails.

## Usage

```csharp
using NotionTaskSync.Tests;

// Example 1: Running all string extension tests in a test suite
public void RunAllStringExtensionTests()
{
    StringExtensionsTestsExtensions.RunTruncateTest();
    StringExtensionsTestsExtensions.RunSanitizeTests();
    StringExtensionsTestsExtensions.RunToSnakeCaseTest();
    StringExtensionsTestsExtensions.RunToSlugTest();
}
```

```csharp
// Example 2: Invoking specific test runners from a console test harness
public static void Main(string[] args)
{
    try 
    {
        Console.WriteLine("Running slug generation tests...");
        StringExtensionsTestsExtensions.RunToSlugTest();
        Console.WriteLine("Slug tests passed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Slug tests failed: {ex.Message}");
    }
}
```

## Notes

*   **Test Isolation:** These methods should be executed in an isolated environment. If the underlying tests modify global state or utilize shared resources (e.g., static configuration), parallel execution of these `Run...` methods may result in non-deterministic failures.
*   **Thread Safety:** The methods themselves do not implement internal locking. They are intended to be executed sequentially by a single-threaded test runner.
*   **Exceptions:** As these are test runners, they are designed to throw standard assertion exceptions (e.g., `Xunit.Sdk.AssertException` or `System.Diagnostics.Debug.Assert` failures) when a test condition is not met. The calling code should be prepared to handle or report these exceptions appropriately.
*   **Input Coverage:** Ensure that these tests are run against a representative sample of input strings, including empty strings, `null` references (if supported by the extension), and strings containing special characters or unicode sequences.
