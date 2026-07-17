#nullable enable

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Provides validation helpers for synchronization configuration objects used in <see cref="SyncServiceTests"/>.
/// Validates null/empty strings, out-of-range numbers, default dates, and other constraints
/// based on the semantic meaning of each member.
/// </summary>
public static class SyncServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="SyncConfig"/> instance.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SyncConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            problems.Add("SyncConfig.Name cannot be null, empty, or whitespace.");
        }
        else if (config.Name.Length > 200)
        {
            problems.Add("SyncConfig.Name exceeds maximum length of 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(config.NotionDatabaseId))
        {
            problems.Add("SyncConfig.NotionDatabaseId cannot be null or empty.");
        }
        else if (config.NotionDatabaseId.Length != 36)
        {
            problems.Add("SyncConfig.NotionDatabaseId must be exactly 36 characters long (GUID format).");
        }

        if (string.IsNullOrWhiteSpace(config.LocalFolderPath))
        {
            problems.Add("SyncConfig.LocalFolderPath cannot be null or empty.");
        }
        else if (!Path.IsPathRooted(config.LocalFolderPath))
        {
            problems.Add("SyncConfig.LocalFolderPath must be an absolute path.");
        }
        else if (!Directory.Exists(Path.GetDirectoryName(config.LocalFolderPath)))
        {
            problems.Add($"SyncConfig.LocalFolderPath directory does not exist: {Path.GetDirectoryName(config.LocalFolderPath)}");
        }

        if (config.SyncIntervalSeconds < 1 || config.SyncIntervalSeconds > 3600)
        {
            problems.Add("SyncConfig.SyncIntervalSeconds must be between 1 and 3600 seconds.");
        }

        if (config.MaxRetries < 0 || config.MaxRetries > 100)
        {
            problems.Add("SyncConfig.MaxRetries must be between 0 and 100.");
        }

        if (config.LastSyncAt.HasValue && config.LastSyncAt.Value > DateTime.UtcNow.AddHours(1))
        {
            problems.Add("SyncConfig.LastSyncAt cannot be in the future.");
        }

        if (config.NextScheduledSyncAt.HasValue && config.NextScheduledSyncAt.Value > DateTime.UtcNow.AddHours(1))
        {
            problems.Add("SyncConfig.NextScheduledSyncAt cannot be in the future.");
        }

        if (config.NextScheduledSyncAt.HasValue && config.LastSyncAt.HasValue)
        {
            if (config.NextScheduledSyncAt.Value <= config.LastSyncAt.Value)
            {
                problems.Add("SyncConfig.NextScheduledSyncAt must be after LastSyncAt.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SyncConfig"/> instance is valid.
    /// </summary>
    /// <param name="config">The configuration to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SyncConfig config) => Validate(config).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="SyncConfig"/> instance is valid.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="config"/> is invalid, containing all validation problems.</exception>
    public static void EnsureValid(this SyncConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var problems = Validate(config);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"SyncConfig is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }

    /// <summary>
    /// Validates the specified <see cref="SyncServiceTests"/> instance.
    /// </summary>
    /// <param name="tests">The tests instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SyncServiceTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var problems = new List<string>();

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SyncServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="tests">The tests instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SyncServiceTests tests) => Validate(tests).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="SyncServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="tests">The tests instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="tests"/> is invalid, containing all validation problems.</exception>
    public static void EnsureValid(this SyncServiceTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var problems = Validate(tests);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"SyncServiceTests instance is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}