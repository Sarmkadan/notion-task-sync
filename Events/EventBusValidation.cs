#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Events;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="EventBus"/> instances.
/// Validates the internal state and configuration of an event bus.
/// </summary>
public static class EventBusValidation
{
    /// <summary>
    /// Validates the specified <see cref="EventBus"/> instance.
    /// Validates that the event bus is in a valid state with no corrupted internal state.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <returns>A list of validation problems; empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate internal subscribers dictionary state
        try
        {
            var subscriberInfo = value.GetSubscriberInfo();

            if (subscriberInfo == null)
            {
                problems.Add("GetSubscriberInfo() returned null dictionary");
            }
            else
            {
                // Check for null or empty keys
                foreach (var kvp in subscriberInfo)
                {
                    if (kvp.Key == null)
                    {
                        problems.Add("Subscriber dictionary contains null event type name");
                    }
                    else if (string.IsNullOrEmpty(kvp.Key))
                    {
                        problems.Add("Subscriber dictionary contains empty event type name");
                    }

                    if (kvp.Value < 0)
                    {
                        problems.Add($"Subscriber count for event type '{kvp.Key}' is negative: {kvp.Value}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            problems.Add($"Failed to validate subscribers: {ex.Message}");
        }

        // Validate Clear operation
        try
        {
            value.Clear();
        }
        catch (Exception ex)
        {
            problems.Add($"Clear() operation failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this EventBus value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the event bus is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this EventBus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"EventBus validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}