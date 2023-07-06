#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Utils;
using FluentAssertions;
using Xunit;

public class StringExtensionsTests
{
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
