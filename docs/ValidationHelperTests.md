# ValidationHelperTests

The `ValidationHelperTests` class provides a suite of unit tests designed to verify the functionality of the `ValidationHelper` class within the `notion-task-sync` project. These tests ensure that data validation logic correctly identifies and rejects invalid inputs while confirming that valid inputs—including Notion IDs, email addresses, file paths, directory paths, and API keys—pass according to the project's established validation criteria.

## API

### Notion ID Validation
*   `IsValidNotionId_WithValidUuidFormat_ReturnsTrue()`: Verifies that a valid UUID-formatted Notion ID (without dashes) returns true.
*   `IsValidNotionId_WithValidUuidFormatWithDashes_ReturnsTrue()`: Verifies that a valid UUID-formatted Notion ID (with dashes) returns true.
*   `IsValidNotionId_WithNullValue_ReturnsFalse()`: Verifies that a null value returns false.
*   `IsValidNotionId_WithEmptyString_ReturnsFalse()`: Verifies that an empty string returns false.
*   `IsValidNotionId_WithTooShortValue_ReturnsFalse()`: Verifies that an input string shorter than the required length returns false.
*   `IsValidNotionId_WithTooLongValue_ReturnsFalse()`: Verifies that an input string longer than the required length returns false.
*   `IsValidNotionId_WithInvalidCharacters_ReturnsFalse()`: Verifies that an input string containing characters outside the allowed set returns false.

### Email Validation
*   `IsValidEmail_WithValidEmailAddress_ReturnsTrue()`: Verifies that a standard, correctly formatted email address returns true.
*   `IsValidEmail_WithMultipleValidFormats_ReturnsTrue()`: Verifies that various valid email address formats return true.
*   `IsValidEmail_WithNullValue_ReturnsFalse()`: Verifies that a null value returns false.
*   `IsValidEmail_WithEmptyString_ReturnsFalse()`: Verifies that an empty string returns false.
*   `IsValidEmail_WithInvalidFormat_ReturnsFalse()`: Verifies that a malformed email address returns false.

### File Path Validation
*   `IsValidFilePath_WithValidPath_ReturnsTrue()`: Verifies that a valid, well-formed file path returns true.
*   `IsValidFilePath_WithNullValue_ReturnsFalse()`: Verifies that a null value returns false.
*   `IsValidFilePath_WithEmptyString_ReturnsFalse()`: Verifies that an empty string returns false.
*   `IsValidFilePath_WithWhitespaceOnly_ReturnsFalse()`: Verifies that a string containing only whitespace returns false.

### Directory Path Validation
*   `IsValidDirectoryPath_WithValidPath_ReturnsTrue()`: Verifies that a valid, well-formed directory path returns true.
*   `IsValidDirectoryPath_WithNullValue_ReturnsFalse()`: Verifies that a null value returns false.
*   `IsValidDirectoryPath_WithEmptyString_ReturnsFalse()`: Verifies that an empty string returns false.

### API Key Validation
*   `IsValidApiKey_WithValidLength_ReturnsTrue()`: Verifies that an API key of the expected, valid length returns true.

## Usage

```csharp
// Example 1: Running the test suite via xUnit
public class ValidationRunner
{
    public void RunTests()
    {
        var tests = new ValidationHelperTests();
        tests.IsValidNotionId_WithValidUuidFormat_ReturnsTrue();
        tests.IsValidEmail_WithValidEmailAddress_ReturnsTrue();
        // Additional tests would be called similarly.
    }
}

// Example 2: Implementing a specific test case within the suite
[Fact]
public void IsValidEmail_WithInvalidFormat_ReturnsFalse()
{
    // Arrange
    string invalidEmail = "invalid-email-format";

    // Act
    bool result = ValidationHelper.IsValidEmail(invalidEmail);

    // Assert
    Assert.False(result);
}
```

## Notes

*   **Edge Cases:** The tests focus heavily on null, empty, and whitespace-only strings to ensure that `ValidationHelper` methods do not throw unexpected exceptions (such as `NullReferenceException`) when receiving deficient inputs.
*   **Thread Safety:** While these test methods themselves are designed to be executed in isolation by a test runner, the thread safety of the underlying `ValidationHelper` logic depends on whether the helper methods maintain any shared mutable state. Assuming the `ValidationHelper` is implemented as a stateless static utility, it is inherently thread-safe.
*   **Test Isolation:** Each test method is designed to be atomic and independent, ensuring that the execution order of these tests does not impact the result.
