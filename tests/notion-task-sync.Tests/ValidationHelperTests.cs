#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Utils;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains unit tests for the <see cref="ValidationHelper"/> class.
/// Tests various validation methods for Notion IDs, emails, file paths, API keys, priorities, and other common validation scenarios.
/// </summary>
public class ValidationHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>true</c> for a valid UUID format without dashes.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithValidUuidFormat_ReturnsTrue()
    {
        // Arrange
        var validId = "550e8400e29b41d4a716446655440000";

        // Act
        var result = ValidationHelper.IsValidNotionId(validId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>true</c> for a valid UUID format that includes dashes.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithValidUuidFormatWithDashes_ReturnsTrue()
    {
        // Arrange
        var validId = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        var result = ValidationHelper.IsValidNotionId(validId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidNotionId(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>false</c> when the input is an empty string.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithEmptyString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidNotionId(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>false</c> for a value that is too short.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithTooShortValue_ReturnsFalse()
    {
        // Arrange
        var shortId = "550e8400";

        // Act
        var result = ValidationHelper.IsValidNotionId(shortId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>false</c> for a value that is too long.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithTooLongValue_ReturnsFalse()
    {
        // Arrange
        var longId = "550e8400-e29b-41d4-a716-446655440000-extra";

        // Act
        var result = ValidationHelper.IsValidNotionId(longId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidNotionId"/> returns <c>false</c> when the value contains invalid characters.
    /// </summary>
    [Fact]
    public void IsValidNotionId_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var invalidId = "550e8400-e29b-41d4-a716-44665544GGGG";

        // Act
        var result = ValidationHelper.IsValidNotionId(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidEmail"/> returns <c>true</c> for a standard valid email address.
    /// </summary>
    [Fact]
    public void IsValidEmail_WithValidEmailAddress_ReturnsTrue()
    {
        // Arrange
        var validEmail = "user@example.com";

        // Act
        var result = ValidationHelper.IsValidEmail(validEmail);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidEmail"/> returns <c>true</c> for several valid email formats, including subdomains and plus addressing.
    /// </summary>
    [Fact]
    public void IsValidEmail_WithMultipleValidFormats_ReturnsTrue()
    {
        var testCases = new[]
        {
            "simple@example.com",
            "user+tag@example.co.uk",
            "firstname.lastname@example.org"
        };

        foreach (var email in testCases)
        {
            ValidationHelper.IsValidEmail(email).Should().BeTrue($"{email} should be valid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidEmail"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidEmail_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidEmail(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidEmail"/> returns <c>false</c> when the input is an empty string.
    /// </summary>
    [Fact]
    public void IsValidEmail_WithEmptyString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidEmail(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidEmail"/> returns <c>false</c> for a variety of malformed email strings.
    /// </summary>
    [Fact]
    public void IsValidEmail_WithInvalidFormat_ReturnsFalse()
    {
        var testCases = new[]
        {
            "notanemail",
            "missing@domain",
            "@nodomain.com",
            "user@",
            "user name@example.com"
        };

        foreach (var email in testCases)
        {
            ValidationHelper.IsValidEmail(email).Should().BeFalse($"{email} should be invalid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidFilePath"/> returns <c>true</c> for a valid file path.
    /// </summary>
    [Fact]
    public void IsValidFilePath_WithValidPath_ReturnsTrue()
    {
        // Arrange
        var validPath = Path.Combine(Path.GetTempPath(), "test.txt");

        // Act
        var result = ValidationHelper.IsValidFilePath(validPath);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidFilePath"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidFilePath_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidFilePath(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidFilePath"/> returns <c>false</c> when the input is an empty string.
    /// </summary>
    [Fact]
    public void IsValidFilePath_WithEmptyString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidFilePath(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidFilePath"/> returns <c>false</c> when the input consists only of whitespace.
    /// </summary>
    [Fact]
    public void IsValidFilePath_WithWhitespaceOnly_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidFilePath("   ").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidDirectoryPath"/> returns <c>true</c> for a valid directory path.
    /// </summary>
    [Fact]
    public void IsValidDirectoryPath_WithValidPath_ReturnsTrue()
    {
        // Arrange
        var validPath = Path.GetTempPath();

        // Act
        var result = ValidationHelper.IsValidDirectoryPath(validPath);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidDirectoryPath"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidDirectoryPath_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidDirectoryPath(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidDirectoryPath"/> returns <c>false</c> when the input is an empty string.
    /// </summary>
    [Fact]
    public void IsValidDirectoryPath_WithEmptyString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidDirectoryPath(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidApiKey"/> returns <c>true</c> for a key of exactly 32 characters.
    /// </summary>
    [Fact]
    public void IsValidApiKey_WithValidLength_ReturnsTrue()
    {
        // Arrange
        var validKey = "a".PadRight(32, 'a'); // 32 characters

        // Act
        var result = ValidationHelper.IsValidApiKey(validKey);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidApiKey"/> returns <c>true</c> for a key of exactly 20 characters, the minimum valid length.
    /// </summary>
    [Fact]
    public void IsValidApiKey_WithMinimumValidLength_ReturnsTrue()
    {
        // Arrange
        var validKey = "a".PadRight(20, 'a'); // Exactly 20 characters

        // Act
        var result = ValidationHelper.IsValidApiKey(validKey);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidApiKey"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidApiKey_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidApiKey(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidApiKey"/> returns <c>false</c> for a key shorter than the minimum length of 20 characters.
    /// </summary>
    [Fact]
    public void IsValidApiKey_WithTooShortValue_ReturnsFalse()
    {
        // Arrange
        var shortKey = "a".PadRight(10, 'a'); // Only 10 characters

        // Act
        var result = ValidationHelper.IsValidApiKey(shortKey);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidPriority"/> returns <c>true</c> for a set of valid priority values within the 0‑100 range.
    /// </summary>
    [Fact]
    public void IsValidPriority_WithValidRange_ReturnsTrue()
    {
        var testCases = new[] { 0, 25, 50, 75, 100 };

        foreach (var priority in testCases)
        {
            ValidationHelper.IsValidPriority(priority).Should().BeTrue($"Priority {priority} should be valid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidPriority"/> returns <c>true</c> for the boundary values 0 and 100.
    /// </summary>
    [Fact]
    public void IsValidPriority_WithBoundaryValues_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsValidPriority(0).Should().BeTrue();
        ValidationHelper.IsValidPriority(100).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidPriority"/> returns <c>false</c> for values outside the 0‑100 range.
    /// </summary>
    [Fact]
    public void IsValidPriority_WithOutOfRangeValues_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidPriority(-1).Should().BeFalse();
        ValidationHelper.IsValidPriority(101).Should().BeFalse();
        ValidationHelper.IsValidPriority(-100).Should().BeFalse();
        ValidationHelper.IsValidPriority(1000).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsInRange"/> returns <c>true</c> for values that lie within the specified inclusive range.
    /// </summary>
    [Fact]
    public void IsInRange_WithValueInRange_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsInRange(50, 0, 100).Should().BeTrue();
        ValidationHelper.IsInRange(0, 0, 100).Should().BeTrue();
        ValidationHelper.IsInRange(100, 0, 100).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsInRange"/> returns <c>false</c> for values outside the specified inclusive range.
    /// </summary>
    [Fact]
    public void IsInRange_WithValueOutOfRange_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsInRange(-1, 0, 100).Should().BeFalse();
        ValidationHelper.IsInRange(101, 0, 100).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>true</c> when the string length falls within the provided minimum and maximum bounds.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithValidLength_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid("Hello", 1, 10).Should().BeTrue();
        ValidationHelper.IsLengthValid("Hello", 5, 10).Should().BeTrue();
        ValidationHelper.IsLengthValid("Hello", 1, 5).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>false</c> when the string is shorter than the minimum required length.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithTooShortString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid("Hi", 3, 10).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>false</c> when the string exceeds the maximum allowed length.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithTooLongString_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid("Hello World", 1, 5).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>true</c> for an empty string when the minimum length is zero.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithEmptyStringAndMinZero_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid(string.Empty, 0, 10).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>true</c> for a <c>null</c> string when the minimum length is zero.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithNullStringAndMinZero_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid(null, 0, 10).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsLengthValid"/> returns <c>false</c> for a <c>null</c> string when the minimum length is greater than zero.
    /// </summary>
    [Fact]
    public void IsLengthValid_WithNullStringAndMinGreaterThanZero_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsLengthValid(null, 1, 10).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.SanitizeString"/> removes control characters from the input string.
    /// </summary>
    [Fact]
    public void SanitizeString_WithControlCharacters_RemovesControlCharacters()
    {
        // Arrange
        var input = "Hello\x00World\x01Test";

        // Act
        var result = ValidationHelper.SanitizeString(input);

        // Assert
        result.Should().Be("HelloWorldTest");
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.SanitizeString"/> trims leading and trailing whitespace.
    /// </summary>
    [Fact]
    public void SanitizeString_WithWhitespaceEdges_TrimsWhitespace()
    {
        // Arrange
        var input = "   Hello World   ";

        // Act
        var result = ValidationHelper.SanitizeString(input);

        // Assert
        result.Should().Be("Hello World");
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.SanitizeString"/> returns an empty string when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void SanitizeString_WithNullValue_ReturnsEmpty()
    {
        // Act
        var result = ValidationHelper.SanitizeString(null);

        // Assert
        result.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.SanitizeString"/> returns an empty string when the input is an empty string.
    /// </summary>
    [Fact]
    public void SanitizeString_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = ValidationHelper.SanitizeString(string.Empty);

        // Assert
        result.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidIdentifierName"/> returns <c>true</c> for valid C# identifier names.
    /// </summary>
    [Fact]
    public void IsValidIdentifierName_WithValidCSharpIdentifier_ReturnsTrue()
    {
        var testCases = new[] { "_private", "PublicName", "snake_case_name", "_123", "CamelCase" };

        foreach (var name in testCases)
        {
            ValidationHelper.IsValidIdentifierName(name).Should().BeTrue($"{name} should be valid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidIdentifierName"/> returns <c>false</c> for invalid identifier names.
    /// </summary>
    [Fact]
    public void IsValidIdentifierName_WithInvalidIdentifier_ReturnsFalse()
    {
        var testCases = new[] { "123start", "Name-With-Dash", "Name With Space", "", "Name.With.Dot" };

        foreach (var name in testCases)
        {
            ValidationHelper.IsValidIdentifierName(name).Should().BeFalse($"{name} should be invalid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidIdentifierName"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidIdentifierName_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidIdentifierName(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidUrl"/> returns <c>true</c> for a simple HTTP URL.
    /// </summary>
    [Fact]
    public void IsValidUrl_WithHttpUrl_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsValidUrl("http://example.com").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidUrl"/> returns <c>true</c> for a simple HTTPS URL.
    /// </summary>
    [Fact]
    public void IsValidUrl_WithHttpsUrl_ReturnsTrue()
    {
        // Act & Assert
        ValidationHelper.IsValidUrl("https://example.com").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidUrl"/> returns <c>true</c> for more complex URLs, including paths, ports, queries, and subdomains.
    /// </summary>
    [Fact]
    public void IsValidUrl_WithComplexUrl_ReturnsTrue()
    {
        var testCases = new[]
        {
            "https://example.com/path",
            "https://example.com:8080/path?query=value",
            "http://subdomain.example.co.uk/path"
        };

        foreach (var url in testCases)
        {
            ValidationHelper.IsValidUrl(url).Should().BeTrue($"{url} should be valid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidUrl"/> returns <c>false</c> for URLs that are not HTTP/HTTPS or are malformed.
    /// </summary>
    [Fact]
    public void IsValidUrl_WithInvalidUrl_ReturnsFalse()
    {
        var testCases = new[] { "ftp://example.com", "not a url", "example.com" };

        foreach (var url in testCases)
        {
            ValidationHelper.IsValidUrl(url).Should().BeFalse($"{url} should be invalid");
        }
    }

    /// <summary>
    /// Verifies that <see cref="ValidationHelper.IsValidUrl"/> returns <c>false</c> when the input is <c>null</c>.
    /// </summary>
    [Fact]
    public void IsValidUrl_WithNullValue_ReturnsFalse()
    {
        // Act & Assert
        ValidationHelper.IsValidUrl(null).Should().BeFalse();
    }
}
