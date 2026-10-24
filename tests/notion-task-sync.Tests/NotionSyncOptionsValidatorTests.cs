#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotionTaskSync.Infrastructure.Configuration;
using Xunit;

/// <summary>
/// Contains unit tests for <see cref="NotionSyncOptionsValidator"/> and the <c>AddNotionTaskSync</c> registration.
/// </summary>
public class NotionSyncOptionsValidatorTests
{
    private const string ValidDatabaseId = "5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a10";

    private static readonly string ValidLocalStorePath =
        Path.GetFullPath(Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "lib", "tasks"));

    private readonly NotionSyncOptionsValidator _validator = new();

    /// <summary>
    /// Verifies that a fully populated, valid set of options passes validation.
    /// </summary>
    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a database ID without dashes is accepted.
    /// </summary>
    [Fact]
    public void Validate_WithDatabaseIdWithoutDashes_ReturnsSuccess()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DatabaseId = "5f1c0a2e7b3d4c8e9a612d4b8e0f7a10";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a missing or blank API token is rejected and the message names <c>ApiToken</c>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingOrBlankApiToken_ReturnsFailureNamingApiToken(string? apiToken)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiToken = apiToken;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:ApiToken").And.Contain("required");
    }

    /// <summary>
    /// Verifies that a database ID that is not a Notion UUID is rejected and the message names <c>DatabaseId</c>.
    /// </summary>
    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a1")]
    [InlineData("5f1c0a2e-7b3d4c8e-9a61-2d4b8e0f7a10")]
    [InlineData("5f1c0a2e7b3d4c8e9a612d4b8e0f7a1g")]
    [InlineData("------------------------------------")]
    public void Validate_WithInvalidDatabaseId_ReturnsFailureNamingDatabaseId(string databaseId)
    {
        // Arrange
        var options = CreateValidOptions();
        options.DatabaseId = databaseId;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:DatabaseId").And.Contain("UUID");
    }

    /// <summary>
    /// Verifies that a missing database ID is rejected and the message names <c>DatabaseId</c>.
    /// </summary>
    [Fact]
    public void Validate_WithMissingDatabaseId_ReturnsFailureNamingDatabaseId()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DatabaseId = null;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:DatabaseId").And.Contain("required");
    }

    /// <summary>
    /// Verifies that a sync interval below one minute is rejected and the message names <c>SyncInterval</c>.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-300)]
    [InlineData(59)]
    public void Validate_WithSyncIntervalBelowOneMinute_ReturnsFailureNamingSyncInterval(int seconds)
    {
        // Arrange
        var options = CreateValidOptions();
        options.SyncInterval = TimeSpan.FromSeconds(seconds);

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:SyncInterval").And.Contain("at least 1 minute");
    }

    /// <summary>
    /// Verifies that an unrecognised conflict strategy is rejected and the message names <c>ConflictStrategy</c>.
    /// </summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("Newest")]
    [InlineData("1")]
    public void Validate_WithUnknownConflictStrategy_ReturnsFailureNamingConflictStrategy(string strategy)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConflictStrategy = strategy;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:ConflictStrategy").And.Contain("LastWrite");
    }

    /// <summary>
    /// Verifies that a relative or missing local store path is rejected and the message names <c>LocalStorePath</c>.
    /// </summary>
    [Theory]
    [InlineData("./tasks")]
    [InlineData("tasks")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithNonAbsoluteLocalStorePath_ReturnsFailureNamingLocalStorePath(string? localStorePath)
    {
        // Arrange
        var options = CreateValidOptions();
        options.LocalStorePath = localStorePath;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("NotionSync:LocalStorePath");
    }

    /// <summary>
    /// Verifies that every invalid property is reported together rather than stopping at the first failure.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleInvalidProperties_ReportsEachFailure()
    {
        // Arrange
        var options = new NotionSyncOptions
        {
            ApiToken = " ",
            DatabaseId = "bad",
            SyncInterval = TimeSpan.Zero,
            ConflictStrategy = "Unknown",
            LocalStorePath = "relative"
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failures.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that <c>AddNotionTaskSync</c> binds the section and that resolving the options throws
    /// <see cref="OptionsValidationException"/> when the configuration is invalid.
    /// </summary>
    [Fact]
    public void AddNotionTaskSync_WithInvalidSection_ThrowsOptionsValidationExceptionOnResolve()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["NotionSync:ApiToken"] = "",
            ["NotionSync:DatabaseId"] = "bad",
            ["NotionSync:SyncInterval"] = "00:00:30",
            ["NotionSync:ConflictStrategy"] = "Unknown",
            ["NotionSync:LocalStorePath"] = "./tasks"
        });
        var services = new ServiceCollection().AddNotionTaskSync(configuration);

        // Act
        var resolve = () => services.BuildServiceProvider().GetRequiredService<IOptions<NotionSyncOptions>>().Value;

        // Assert
        resolve.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that <c>AddNotionTaskSync</c> resolves bound options from a valid configuration section.
    /// </summary>
    [Fact]
    public void AddNotionTaskSync_WithValidSection_ResolvesBoundOptions()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["NotionSync:ApiToken"] = "test-token",
            ["NotionSync:DatabaseId"] = ValidDatabaseId,
            ["NotionSync:SyncInterval"] = "00:10:00",
            ["NotionSync:ConflictStrategy"] = "LocalWins",
            ["NotionSync:LocalStorePath"] = ValidLocalStorePath
        });
        var services = new ServiceCollection().AddNotionTaskSync(configuration);

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<NotionSyncOptions>>().Value;

        // Assert
        options.DatabaseId.Should().Be(ValidDatabaseId);
        options.SyncInterval.Should().Be(TimeSpan.FromMinutes(10));
        options.ConflictStrategy.Should().Be("LocalWins");
        options.LocalStorePath.Should().Be(ValidLocalStorePath);
    }

    private static NotionSyncOptions CreateValidOptions() => new()
    {
        ApiToken = "test-token",
        DatabaseId = ValidDatabaseId,
        SyncInterval = TimeSpan.FromMinutes(5),
        ConflictStrategy = "LastWrite",
        LocalStorePath = ValidLocalStorePath
    };

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
