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
/// Tests for the string extension methods defined in <see cref="NotionTaskSync.Utils.StringExtensions"/>.
/// </summary>
public class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.Truncate(string,int)"/> truncates a string longer than the specified maximum length
    /// and appends the default suffix ("...") to produce a string of the exact maximum length.
    /// </summary>
    [Fact]
    public void Truncate_StringLongerThanMaxLength_ReturnsTruncatedWithDefaultSuffix()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = input.Truncate(8);

        // Assert
        result.Should().Be("Hello...");
        result.Length.Should().Be(8);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SanitizeForFilename(string)"/> returns the string "untitled"
    /// when the input is an empty string.
    /// </summary>
    [Fact]
    public void SanitizeForFilename_EmptyString_ReturnsUntitled()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.SanitizeForFilename();

        // Assert
        result.Should().Be("untitled");
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SanitizeForFilename(string)"/> replaces spaces with underscores
    /// while preserving other characters.
    /// </summary>
    [Fact]
    public void SanitizeForFilename_StringWithSpaces_ReplacesSpacesWithUnderscores()
    {
        // Arrange
        var input = "My Task File";

        // Act
        var result = input.SanitizeForFilename();

        // Assert
        result.Should().Be("My_Task_File");
        result.Should().NotContain(" ");
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSnakeCase(string)"/> converts a PascalCase string
    /// into a lowercase snake_case representation.
    /// </summary>
    [Fact]
    public void ToSnakeCase_PascalCaseString_ReturnsLowercaseWithUnderscores()
    {
        // Arrange
        var input = "NotionTaskSync";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be("notion_task_sync");
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string)"/> removes punctuation, replaces spaces with hyphens,
    /// and returns a lowercase slug that matches the regex pattern ^[a-z0-9\-]+$.
    /// </summary>
    [Fact]
    public void ToSlug_StringWithPunctuationAndSpaces_ReturnsCleanHyphenatedSlug()
    {
        // Arrange
        var input = "Hello World!";

        // Act
        var result = input.ToSlug();

        // Assert
        result.Should().Be("hello-world");
        result.Should().MatchRegex(@"^[a-z0-9\-]+$");
    }
}
