#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Workers;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides validation helpers for <see cref="HealthCheckWorker"/> and <see cref="HealthStatus"/> classes.
/// Validates that health check data is within expected ranges and not in default/empty states.
/// </summary>
public static class HealthCheckWorkerValidation
{
    /// <summary>
    /// Validates a <see cref="HealthCheckWorker"/> instance.
    /// </summary>
    /// <param name="value">The health check worker to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckWorker value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate memory usage (should be reasonable for a background worker)
        if (value.MemoryUsageMb < 0)
        {
            problems.Add($"MemoryUsageMb must be non-negative, but was {value.MemoryUsageMb}.");
        }
        else if (value.MemoryUsageMb > 10_000) // 10 GB sanity check
        {
            problems.Add($"MemoryUsageMb {value.MemoryUsageMb} appears excessively high (>10GB).");
        }

        // Validate thread count (should be reasonable for a .NET application)
        if (value.ThreadCount < 0)
        {
            problems.Add($"ThreadCount must be non-negative, but was {value.ThreadCount}.");
        }
        else if (value.ThreadCount > 1_000) // 1000 threads is excessive
        {
            problems.Add($"ThreadCount {value.ThreadCount} appears excessively high (>1000 threads).");
        }

        // Validate uptime (should be positive after startup)
        if (value.UptimeSeconds < 0)
        {
            problems.Add($"UptimeSeconds must be non-negative, but was {value.UptimeSeconds}.");
        }
        else if (value.UptimeSeconds > 86_400 * 365 * 10) // 10 years in seconds
        {
            problems.Add($"UptimeSeconds {value.UptimeSeconds} appears excessively high (>10 years).");
        }

        // Validate CheckedAt (should not be default DateTime)
        if (value.CheckedAt == default)
        {
            problems.Add("CheckedAt must be set to a valid DateTime, but was default.");
        }
        else if (value.CheckedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add($"CheckedAt {value.CheckedAt:g} appears to be in the future.");
        }
        else if (value.CheckedAt < DateTime.UtcNow.AddDays(-7))
        {
            problems.Add($"CheckedAt {value.CheckedAt:g} appears to be older than 7 days.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="HealthStatus"/> instance.
    /// </summary>
    /// <param name="value">The health status to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate IsHealthy (should be consistent with actual metrics)
        if (!value.IsHealthy && value.MemoryUsageMb >= 0 && value.ThreadCount >= 0)
        {
            // If marked unhealthy but metrics are valid, check consistency
            var expectedHealthy = value.MemoryUsageMb < 500 && value.ThreadCount < 50;
            if (expectedHealthy)
            {
                problems.Add(string.Format(
                    "IsHealthy is false but memory usage ({0}MB) and thread count ({1}) are within acceptable ranges (<500MB, <50 threads).",
                    value.MemoryUsageMb,
                    value.ThreadCount));
            }
        }

        // Validate MemoryUsageMb
        if (value.MemoryUsageMb < 0)
        {
            problems.Add($"MemoryUsageMb must be non-negative, but was {value.MemoryUsageMb}.");
        }
        else if (value.MemoryUsageMb > 10_000) // 10 GB sanity check
        {
            problems.Add($"MemoryUsageMb {value.MemoryUsageMb} appears excessively high (>10GB).");
        }

        // Validate ThreadCount
        if (value.ThreadCount < 0)
        {
            problems.Add($"ThreadCount must be non-negative, but was {value.ThreadCount}.");
        }
        else if (value.ThreadCount > 1_000) // 1000 threads is excessive
        {
            problems.Add($"ThreadCount {value.ThreadCount} appears excessively high (>1000 threads).");
        }

        // Validate UptimeSeconds
        if (value.UptimeSeconds < 0)
        {
            problems.Add($"UptimeSeconds must be non-negative, but was {value.UptimeSeconds}.");
        }
        else if (value.UptimeSeconds > 86_400 * 365 * 10) // 10 years in seconds
        {
            problems.Add($"UptimeSeconds {value.UptimeSeconds} appears excessively high (>10 years).");
        }

        // Validate CheckedAt
        if (value.CheckedAt == default)
        {
            problems.Add("CheckedAt must be set to a valid DateTime, but was default.");
        }
        else if (value.CheckedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add($"CheckedAt {value.CheckedAt:g} appears to be in the future.");
        }
        else if (value.CheckedAt < DateTime.UtcNow.AddDays(-7))
        {
            problems.Add($"CheckedAt {value.CheckedAt:g} appears to be older than 7 days.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="HealthCheckWorker"/> instance is valid.
    /// </summary>
    /// <param name="value">The health check worker to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this HealthCheckWorker value) => value.Validate().Count == 0;

    /// <summary>
    /// Determines whether a <see cref="HealthStatus"/> instance is valid.
    /// </summary>
    /// <param name="value">The health status to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this HealthStatus value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="HealthCheckWorker"/> instance is valid.
    /// </summary>
    /// <param name="value">The health check worker to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing the validation problems.</exception>
    public static void EnsureValid(this HealthCheckWorker value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"HealthCheckWorker validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    /// <summary>
    /// Ensures that a <see cref="HealthStatus"/> instance is valid.
    /// </summary>
    /// <param name="value">The health status to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing the validation problems.</exception>
    public static void EnsureValid(this HealthStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"HealthStatus validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }
}