#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Domain.Exceptions;

using System;

/// <summary>
/// Base exception for sync-related errors in the application.
/// Provides context about sync operations that failed.
/// </summary>
public class SyncException : Exception
{
    public SyncException(string message) : base(message) { }

    public SyncException(string message, Exception innerException)
        : base(message, innerException) { }

    public string? SyncConfigId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }

    /// <summary>
    /// Creates a sync exception with detailed context information.
    /// </summary>
    public static SyncException CreateWithContext(string message, string? configId = null, string? details = null)
    {
        return new SyncException(message)
        {
            SyncConfigId = configId,
            Details = details
        };
    }
}

/// <summary>
/// Exception raised when Notion API operations fail.
/// </summary>
public class NotionApiException : SyncException
{
    public NotionApiException(string message) : base(message) { }

    public NotionApiException(string message, Exception innerException)
        : base(message, innerException) { }

    public int? HttpStatusCode { get; set; }

    public string? ApiErrorCode { get; set; }
}

/// <summary>
/// Exception raised when local file I/O operations fail.
/// </summary>
public class LocalFileException : SyncException
{
    public LocalFileException(string message) : base(message) { }

    public LocalFileException(string message, Exception innerException)
        : base(message, innerException) { }

    public string? FilePath { get; set; }
}

/// <summary>
/// Exception raised when validation of entities or operations fails.
/// </summary>
public class ValidationException : SyncException
{
    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    public string? FieldName { get; set; }

    public object? InvalidValue { get; set; }
}

/// <summary>
/// Exception raised when a conflict cannot be resolved automatically.
/// </summary>
public class ConflictException : SyncException
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException) { }

    public Guid? TaskId { get; set; }

    public string? ConflictDetails { get; set; }
}

/// <summary>
/// Exception raised when configuration is invalid or incomplete.
/// </summary>
public class ConfigurationException : SyncException
{
    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }

    public string? ConfigurationKey { get; set; }
}
