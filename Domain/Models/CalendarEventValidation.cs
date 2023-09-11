#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Models;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="CalendarEvent"/> instances.
/// </summary>
public static class CalendarEventValidation
{
    /// <summary>
    /// Validates all properties of a <see cref="CalendarEvent"/> and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The calendar event to validate.</param>
    /// <returns>An immutable list of validation error messages. Empty if the event is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CalendarEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (value.Id == Guid.Empty)
        {
            errors.Add("Id must be a non-empty GUID.");
        }

        // Validate Title
        if (string.IsNullOrWhiteSpace(value.Title))
        {
            errors.Add("Title is required and cannot be empty or whitespace.");
        }
        else if (value.Title.Length > 500)
        {
            errors.Add("Title cannot exceed 500 characters.");
        }

        // Validate Description
        if (value.Description?.Length > 5000)
        {
            errors.Add("Description cannot exceed 5000 characters.");
        }

        // Validate StartDate
        if (value.StartDate == default)
        {
            errors.Add("StartDate must be set to a valid DateTime.");
        }
        else if (value.StartDate.Kind != DateTimeKind.Utc)
        {
            errors.Add("StartDate must be in UTC format.");
        }
        else if (value.StartDate < DateTime.UtcNow.AddYears(-10))
        {
            errors.Add("StartDate cannot be more than 10 years in the past.");
        }
        else if (value.StartDate > DateTime.UtcNow.AddYears(10))
        {
            errors.Add("StartDate cannot be more than 10 years in the future.");
        }

        // Validate EndDate
        if (value.EndDate.HasValue)
        {
            if (value.EndDate.Value == default)
            {
                errors.Add("EndDate must be set to a valid DateTime if provided.");
            }
            else if (value.EndDate.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("EndDate must be in UTC format.");
            }
            else if (value.EndDate.Value < value.StartDate)
            {
                errors.Add("EndDate cannot be earlier than StartDate.");
            }
            else if (value.EndDate.Value > DateTime.UtcNow.AddYears(10))
            {
                errors.Add("EndDate cannot be more than 10 years in the future.");
            }
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            errors.Add("CreatedAt must be set to a valid DateTime.");
        }
        else if (value.CreatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add("CreatedAt must be in UTC format.");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAt cannot be in the future.");
        }

        // Validate UpdatedAt
        if (value.UpdatedAt == default)
        {
            errors.Add("UpdatedAt must be set to a valid DateTime.");
        }
        else if (value.UpdatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add("UpdatedAt must be in UTC format.");
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("UpdatedAt cannot be in the future.");
        }

        // Validate Source
        // No validation needed for enum - it has a default value

        // Validate Location
        if (value.Location?.Length > 500)
        {
            errors.Add("Location cannot exceed 500 characters.");
        }

        // Validate ExternalUid
        if (value.ExternalUid?.Length > 256)
        {
            errors.Add("ExternalUid cannot exceed 256 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CalendarEvent"/> is valid.
    /// </summary>
    /// <param name="value">The calendar event to check.</param>
    /// <returns>True if the event is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this CalendarEvent? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CalendarEvent"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with detailed error messages if it is not.
    /// </summary>
    /// <param name="value">The calendar event to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the event fails validation.</exception>
    public static void EnsureValid(this CalendarEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"CalendarEvent validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}