#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Utils;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for validation-related data structures.
/// Enables JSON serialization of validation results and validation state.
/// </summary>
public static class ValidationHelperJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a validation result to a JSON string.
    /// </summary>
    /// <param name="result">The validation result to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static string ToJson(this ValidationResult result, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ValidationResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="ValidationResult"/> instance deserialized from JSON.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ValidationResult? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<ValidationResult>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ValidationResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">Receives the deserialized instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ValidationResult? result)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            result = JsonSerializer.Deserialize<ValidationResult>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation succeeded.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets the validation error message, if any.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the validated value.
        /// </summary>
        public object? Value { get; init; }

        /// <summary>
        /// Gets the type of validation that was performed.
        /// </summary>
        public string? ValidationType { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        [JsonConstructor]
        public ValidationResult()
        {
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <param name="value">The validated value.</param>
        /// <param name="validationType">The type of validation performed.</param>
        /// <returns>A new <see cref="ValidationResult"/> instance.</returns>
        public static ValidationResult Success(object? value, string? validationType = null)
        {
            return new ValidationResult
            {
                IsValid = true,
                Value = value,
                ValidationType = validationType
            };
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why validation failed.</param>
        /// <param name="value">The value that failed validation.</param>
        /// <param name="validationType">The type of validation performed.</param>
        /// <returns>A new <see cref="ValidationResult"/> instance.</returns>
        public static ValidationResult Failure(string errorMessage, object? value = null, string? validationType = null)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
                Value = value,
                ValidationType = validationType
            };
        }
    }
}