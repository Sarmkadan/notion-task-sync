# StringExtensionsTests

Test class that verifies the behavior of extension methods defined in `StringExtensions`. Each public method corresponds to a specific scenario for one of the extension methods (e.g., `Truncate`, `SanitizeForFilename`, `ToSnakeCase`, `ToSlug`) and asserts the expected output.

## API

### `public void Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix`
- **Purpose**: Confirms that the `Truncate` extension method correctly shortens a string that exceeds the specified maximum length and appends the default suffix (`"..."`).
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: Throws an exception of type `AssertFailedException` (or the testing framework's equivalent) if the truncated result does not match the expected value.

### `public void SanitizeForFilename_EmptyString_ReturnsUntitled`
- **Purpose**: Verifies that calling `SanitizeForFilename` on an empty input string yields the fallback value `"Untitled"`.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: Throws an exception if the result is not `"Untitled"`.

### `public void Sanit not equal `"Untitled"`.

### `public void SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores`
- **Purpose**: Ensures that spaces within the input string are replaced by underscores when `SanitizeForFilename` is invoked.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: Throws an exception if any space remains in the output or if other unexpected transformations occur.

### `public void ToSnakeCase_PascalCaseString_ReturnsLowercaseWithUnderscores`
- **Purpose**: Checks that a PascalCase string is converted to lowercase snake_case (e.g., `"HelloWorld"` → `"hello_world"`).
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: Throws an exception if the conversion does not produce the expected snake_case string.

### `public void ToSlug_StringWithPunctuationAndSpaces_ReturnsCleanHyphenatedSlug`
- **Purpose**: Validates that punctuation is removed, spaces are replaced with hyphens, and the result is lowercased when `ToSlug` is applied.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: Throws an exception if the resulting slug contains punctuation, incorrect spacing, or incorrect casing.

## Usage

The test class is intended to be executed by a unit‑test runner (e.g., xUnit, NUnit, MSTest). Below are two examples showing how individual test methods can be invoked manually in a test setup.

```csharp
using NUnit.Framework; // or appropriate test framework
using NotionTaskSync.Tests; // namespace containing StringExtensionsTests

[TestFixture]
public class StringExtensionsTestsRunner
{
    [Test]
    public void RunTruncateTest()
    {
        var testInstance = new StringExtensionsTests();
        // Act
        testInstance.Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix();
        // If no exception is thrown, the test passed.
    }

    [Test]
    public void RunSanitizeSpacesTest()
    {
        var testInstance = new StringExtensionsTests();
        // Act
        testInstance.SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores();
        // Assert implicitly via the method's internal assertions.
    }
}
```

## Notes

- **Edge Cases**: Each test method encapsulates its own edge‑case verification (e.g., empty strings, strings exactly at the length boundary, strings containing only punctuation). The implementation of the extension methods themselves should handle `null` inputs gracefully; however, these particular tests do not cover `null` because they focus on the scenarios listed.
- **Thread‑Safety**: The test methods contain no mutable shared state; they operate only on local variables and immutable literals. Consequently, they are safe to execute concurrently in parallel test runs without risk of race conditions. The extension methods under test are also pure functions (no side effects), further supporting thread‑safe usage.
