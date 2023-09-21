#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Infrastructure.Configuration;
using NotionTaskSync.Services;

/// <summary>
/// AdvancedUsageExtensions.cs - Extension methods for AdvancedUsage class
///
/// Provides additional functionality for advanced sync scenarios including:
/// - Configuration validation and diagnostics
/// - Performance monitoring and optimization
/// - Conflict resolution utilities
/// - Sync result analysis and reporting
/// </summary>
public static class AdvancedUsageExtensions
{
    /// <summary>
    /// Validates the sync configuration and returns detailed validation report.
    /// </summary>
    /// <param name="config">The sync configuration to validate</param>
    /// <param name="logger">Optional logger for validation messages</param>
    /// <returns>Validation report with issues and recommendations</returns>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is null.</exception>
    public static SyncConfigValidationReport ValidateConfiguration(this SyncConfig config, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var report = new SyncConfigValidationReport
        {
            IsValid = true,
            Issues = new List<string>(),
            Warnings = new List<string>(),
            Recommendations = new List<string>()
        };

        // Validate required fields
        if (string.IsNullOrWhiteSpace(config.NotionDatabaseId))
        {
            report.IsValid = false;
            report.Issues.Add("NotionDatabaseId is required but is null or empty");
            report.Recommendations.Add("Set NotionDatabaseId to a valid Notion database ID");
        }

        if (string.IsNullOrWhiteSpace(config.LocalFolderPath))
        {
            report.IsValid = false;
            report.Issues.Add("LocalFolderPath is required but is null or empty");
            report.Recommendations.Add("Set LocalFolderPath to a valid local directory path");
        }

        // Validate sync direction
        if (config.Direction == SyncDirection.Unknown)
        {
            report.Warnings.Add("SyncDirection is set to Unknown - defaulting to Bidirectional");
            report.Recommendations.Add("Explicitly set SyncDirection to NotionToLocal, LocalToNotion, or Bidirectional");
        }

        // Validate conflict resolution
        if (config.ConflictStrategy == ConflictResolutionStrategy.Unknown)
        {
            report.Warnings.Add("ConflictStrategy is set to Unknown - defaulting to LocalWins");
            report.Recommendations.Add("Set ConflictStrategy to LocalWins, NotionWins, or Custom");
        }

        // Validate sync interval
        if (config.SyncIntervalSeconds < 30)
        {
            report.Warnings.Add($"SyncIntervalSeconds ({config.SyncIntervalSeconds}) is very low - may cause rate limiting");
            report.Recommendations.Add("Increase SyncIntervalSeconds to at least 60 for production use");
        }

        // Validate field mappings
        if (config.FieldMappings == null || config.FieldMappings.Count == 0)
        {
            report.Warnings.Add("No field mappings configured - using default property names");
            report.Recommendations.Add("Configure FieldMappings for custom field name translations");
        }

        // Log validation results
        if (logger != null)
        {
            if (report.IsValid)
            {
                logger.LogInformation("✅ Configuration validation passed");
            }
            else
            {
                logger.LogWarning("⚠️ Configuration validation failed with {Count} issues", report.Issues.Count);
            }

            foreach (var issue in report.Issues)
            {
                logger.LogError(issue);
            }

            foreach (var warning in report.Warnings)
            {
                logger.LogWarning(warning);
            }
        }

        return report;
    }

    /// <summary>
    /// Creates a performance-optimized configuration with recommended settings.
    /// </summary>
    /// <param name="name">Name for the sync configuration</param>
    /// <param name="notionDatabaseId">Notion database ID</param>
    /// <param name="localFolderPath">Local folder path</param>
    /// <param name="logger">Optional logger for configuration messages</param>
    /// <returns>Optimized SyncConfig instance</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="notionDatabaseId"/> or <paramref name="localFolderPath"/> is null.</exception>
    public static SyncConfig CreateOptimizedConfiguration(
        this string name,
        string notionDatabaseId,
        string localFolderPath,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(notionDatabaseId);
        ArgumentNullException.ThrowIfNull(localFolderPath);

        logger?.LogInformation("Creating optimized configuration for {Name}", name);

        var config = new SyncConfig(name, notionDatabaseId, localFolderPath)
        {
            Direction = SyncDirection.Bidirectional,
            ConflictStrategy = ConflictResolutionStrategy.SmartMerge,
            SyncIntervalSeconds = 300, // 5 minutes
            IsEnabled = true,
            EnableCaching = true,
            EnableCompression = true
        };

        // Recommended field mappings for common task management
        config.FieldMappings = new Dictionary<string, string>
        {
            { "title", "Title" },
            { "status", "Status" },
            { "priority", "Priority" },
            { "dueDate", "Due Date" },
            { "assignee", "Assignee" },
            { "description", "Description" },
            { "tags", "Tags" },
            { "project", "Project" },
            { "createdDate", "Created time" },
            { "lastEdited", "Last edited time" }
        };

        // Ignore internal fields
        config.IgnoredFields = new List<string> { "internalId", "createdBy", "lastEditedBy" };

        // Smart conflict resolution for common fields
        config.FieldConflictStrategies = new Dictionary<string, ConflictResolutionStrategy>
        {
            { "description", ConflictResolutionStrategy.LocalWins },
            { "notes", ConflictResolutionStrategy.LocalWins },
            { "attachments", ConflictResolutionStrategy.LocalWins },
            { "status", ConflictResolutionStrategy.SmartMerge },
            { "priority", ConflictResolutionStrategy.SmartMerge },
            { "dueDate", ConflictResolutionStrategy.SmartMerge },
            { "tags", ConflictResolutionStrategy.MergeAdditive }
        };

        logger?.LogDebug("Optimized configuration created with {Mappings} field mappings", config.FieldMappings?.Count);

        return config;
    }

    /// <summary>
    /// Analyzes sync results and provides detailed performance metrics.
    /// </summary>
    /// <param name="result">Sync result to analyze</param>
    /// <param name="logger">Optional logger for analysis messages</param>
    /// <returns>Analysis report with performance metrics</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static SyncAnalysisReport AnalyzeResults(this SyncService.SyncResult result, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var report = new SyncAnalysisReport
        {
            TotalTasks = result.LocalTaskCount,
            SyncedTasks = result.SyncedCount,
            Conflicts = result.ConflictsDetected,
            DurationMs = result.Duration,
            SuccessRate = result.LocalTaskCount > 0
                ? (double)result.SyncedCount / result.LocalTaskCount * 100
                : 0
        };

        // Calculate performance metrics
        if (result.Duration > 0)
        {
            report.TasksPerSecond = result.LocalTaskCount / (result.Duration / 1000.0);
            report.AvgProcessingTimeMs = result.Duration / Math.Max(1, result.SyncedCount);
        }

        // Calculate conflict rate
        report.ConflictRate = result.ConflictsDetected > 0 && result.LocalTaskCount > 0
            ? (double)result.ConflictsDetected / result.LocalTaskCount * 100
            : 0;

        // Determine sync efficiency rating using pattern matching
        report.EfficiencyRating = report.TasksPerSecond switch
        {
            > 10 => "Excellent",
            > 5 => "Good",
            > 2 => "Fair",
            _ => "Poor"
        };

        // Log analysis results
        logger?.LogInformation("📊 Sync Analysis: {Rating} efficiency ({TasksPerSecond:F2} tasks/sec)",
            report.EfficiencyRating, report.TasksPerSecond);

        logger?.LogInformation("📈 Success rate: {SuccessRate:F1}% | Conflict rate: {ConflictRate:F1}%",
            report.SuccessRate, report.ConflictRate);

        if (report.Conflicts > 0)
        {
            logger?.LogWarning("⚠️ {Conflicts} conflicts detected - review recommended", report.Conflicts);
        }

        return report;
    }

    /// <summary>
    /// Executes sync with automatic retry on transient failures.
    /// </summary>
    /// <param name="syncService">Sync service instance</param>
    /// <param name="config">Sync configuration</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="retryDelayMs">Delay between retries in milliseconds</param>
    /// <param name="logger">Optional logger for retry messages</param>
    /// <returns>Sync result with retry information</returns>
    /// <exception cref="ArgumentNullException"><paramref name="syncService"/> or <paramref name="config"/> is null.</exception>
    /// <exception cref="SyncFailedException">Thrown when all retry attempts fail.</exception>
    public static async Task<SyncService.SyncResult> ExecuteWithRetryAsync(
        this SyncService syncService,
        SyncConfig config,
        int maxRetries = 3,
        int retryDelayMs = 2000,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(syncService);
        ArgumentNullException.ThrowIfNull(config);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetries, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelayMs, 0);

        SyncService.SyncResult result = null!;
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                logger?.LogInformation("🔄 Sync attempt {Attempt}/{MaxRetries} for {ConfigName}",
                    retryCount + 1, maxRetries + 1, config.Name);

                result = await syncService.ExecuteSyncAsync(config);
                logger?.LogInformation("✅ Sync completed successfully on attempt {Attempt}", retryCount + 1);
                break;
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                lastException = ex;
                retryCount++;

                if (retryCount <= maxRetries)
                {
                    logger?.LogWarning("⚠️ Transient error on attempt {Attempt}: {Error}. Retrying in {Delay}ms...",
                        retryCount, ex.Message, retryDelayMs);
                    await Task.Delay(retryDelayMs);
                }
            }
        }

        if (retryCount > maxRetries && result == null)
        {
            logger?.LogError("❌ Sync failed after {MaxRetries} attempts. Last error: {Error}",
                maxRetries, lastException?.Message);
            throw new SyncFailedException($"Sync failed after {maxRetries} retry attempts", lastException);
        }

        return result;
    }

    /// <summary>
    /// Determines if an exception represents a transient/retriable error.
    /// </summary>
    /// <param name="ex">Exception to check</param>
    /// <returns>True if the exception is transient; otherwise false.</returns>
    private static bool IsTransientError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        return ex is NotionApiException apiEx && apiEx.StatusCode >= 500
            || ex is TimeoutException
            || ex is OperationCanceledException
            || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Represents a validation report for sync configuration.
/// </summary>
public class SyncConfigValidationReport
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Represents an analysis report for sync results.
/// </summary>
public class SyncAnalysisReport
{
    public int TotalTasks { get; set; }
    public int SyncedTasks { get; set; }
    public int Conflicts { get; set; }
    public double DurationMs { get; set; }
    public double TasksPerSecond { get; set; }
    public double AvgProcessingTimeMs { get; set; }
    public double SuccessRate { get; set; }
    public double ConflictRate { get; set; }
    public string EfficiencyRating { get; set; } = "Unknown";
}

/// <summary>
/// Exception thrown when sync fails after all retry attempts.
/// </summary>
public class SyncFailedException : Exception
{
    public SyncFailedException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}