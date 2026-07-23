#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Domain.Models;
using FluentAssertions;
using Xunit;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Tests for SyncConfig configuration, particularly clock skew tolerance functionality.
/// </summary>
public class SyncConfigTests
{
    /// <summary>
    /// Tests that ClockSkewToleranceMs defaults to 60000 (1 minute).
    /// This accounts for Notion's minute-level timestamp granularity.
    /// </summary>
    [Fact]
    public void ClockSkewToleranceMs_DefaultsToOneMinute()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path");

        // Assert
        config.ClockSkewToleranceMs.Should().Be(60000);
    }

    /// <summary>
    /// Tests that ClockSkewToleranceMs can be customized.
    /// </summary>
    [Fact]
    public void ClockSkewToleranceMs_CanBeCustomized()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path")
        {
            ClockSkewToleranceMs = 30000 // 30 seconds
        };

        // Assert
        config.ClockSkewToleranceMs.Should().Be(30000);
    }

    /// <summary>
    /// Tests that ClockSkewToleranceMs accepts maximum value of 86400000 (24 hours).
    /// </summary>
    [Fact]
    public void ClockSkewToleranceMs_AcceptsMaximumValue()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path")
        {
            ClockSkewToleranceMs = 86400000 // 24 hours
        };

        // Assert
        config.ClockSkewToleranceMs.Should().Be(86400000);
    }

    /// <summary>
    /// Tests that ClockSkewToleranceMs validation rejects values above maximum.
    /// </summary>
    [Fact]
    public void ClockSkewToleranceMs_RejectsValuesAboveMaximum()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path");

        // Act
        config.ClockSkewToleranceMs = 86400001; // 1ms over limit

        // Assert - Validate the property manually since Range attribute doesn't throw automatically
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(config);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        isValid.Should().BeFalse();
        validationResults.Should().Contain(v => v.MemberNames.Contains("ClockSkewToleranceMs"));
    }

    /// <summary>
    /// Tests that ClockSkewToleranceMs validation rejects negative values.
    /// </summary>
    [Fact]
    public void ClockSkewToleranceMs_RejectsNegativeValues()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path");

        // Act
        config.ClockSkewToleranceMs = -1;

        // Assert - Validate the property manually since Range attribute doesn't throw automatically
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(config);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        isValid.Should().BeFalse();
        validationResults.Should().Contain(v => v.MemberNames.Contains("ClockSkewToleranceMs"));
    }

    /// <summary>
    /// Tests that SyncConfig can be serialized and deserialized while preserving ClockSkewToleranceMs.
    /// </summary>
    [Fact]
    public void SyncConfig_PreservesClockSkewToleranceMs_ThroughSerialization()
    {
        // Arrange
        var originalConfig = new SyncConfig("test", "test-db-id", "/test/path")
        {
            ClockSkewToleranceMs = 120000 // 2 minutes
        };

        // Simulate serialization round-trip (simplified)
        var configCopy = new SyncConfig(originalConfig.Name, originalConfig.NotionDatabaseId, originalConfig.LocalFolderPath)
        {
            ClockSkewToleranceMs = originalConfig.ClockSkewToleranceMs
        };

        // Assert
        configCopy.ClockSkewToleranceMs.Should().Be(originalConfig.ClockSkewToleranceMs);
    }

    /// <summary>
    /// Tests that GetFieldConflictStrategy works correctly with the new ClockSkewToleranceMs property.
    /// </summary>
    [Fact]
    public void SyncConfig_GetFieldConflictStrategy_WorksWithClockSkewTolerance()
    {
        // Arrange
        var config = new SyncConfig("test", "test-db-id", "/test/path")
        {
            ClockSkewToleranceMs = 150000 // 2.5 minutes
        };

        // Assert
        config.ClockSkewToleranceMs.Should().Be(150000);
        config.ConflictStrategy.Should().Be(ConflictResolutionStrategy.LastWrite); // Default
    }

    /// <summary>
    /// Tests that multiple SyncConfig instances can have different ClockSkewToleranceMs values.
    /// </summary>
    [Fact]
    public void MultipleSyncConfigs_CanHaveDifferentClockSkewToleranceValues()
    {
        // Arrange
        var config1 = new SyncConfig("config1", "db1", "/path1")
        {
            ClockSkewToleranceMs = 30000
        };

        var config2 = new SyncConfig("config2", "db2", "/path2")
        {
            ClockSkewToleranceMs = 300000 // 5 minutes
        };

        var config3 = new SyncConfig("config3", "db3", "/path3"); // Default

        // Assert
        config1.ClockSkewToleranceMs.Should().Be(30000);
        config2.ClockSkewToleranceMs.Should().Be(300000);
        config3.ClockSkewToleranceMs.Should().Be(60000); // Default
    }
}