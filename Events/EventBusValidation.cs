#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="EventBus"/> instances.
/// Validates the internal state and configuration of an event bus.
/// </summary>
public static class EventBusValidation
{
    /// <summary>
    /// Validates the specified <see cref="EventBus"/> instance.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <returns>A list of validation problems; empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate EventBus internal state
        // Note: EventBus doesn't expose its internal _subscribers field directly,
        // so we can only validate the public API behavior

        try
        {
            // Test that basic operations don't throw
            _ = value.GetSubscriberInfo();
            value.Clear();
        }
        catch (Exception ex)
        {
            problems.Add($"EventBus operations failed: {ex.Message}");
        }

        // Validate that EventId is not empty Guid (if accessible through reflection)
        // Note: EventBus doesn't expose EventId directly, so this validation
        // would apply to ApplicationEvent instances, not EventBus itself

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this EventBus value)
    {
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