#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using System;
using System.Collections.Generic;

/// <summary>
/// Notion API configuration settings.
/// Contains API authentication, endpoints, and rate limiting configuration.
/// </summary>
public sealed class NotionApiSettings
{
    /// <summary>
    /// Gets or sets the Notion API authentication token.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Notion API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.notion.com/v1";

    /// <summary>
    /// Gets or sets the Notion API version.
    /// </summary>
    public string ApiVersion { get; set; } = "2022-06-28";

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the rate limit threshold (requests per minute).
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to respect Notion's rate limiting headers.
    /// </summary>
    public bool RespectRateLimits { get; set; } = true;

    /// <summary>
    /// Gets or sets the default page size for queries.
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum page size for queries.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to cache API responses.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the list of database IDs to sync.
    /// </summary>
    public List<string> DatabaseIds { get; set; } = new();

    /// <summary>
    /// Gets or sets custom property mappings for field translation.
    /// </summary>
    public Dictionary<string, string> PropertyMappings { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of task statuses to include in sync (empty = all statuses).
        /// </summary>
        public List<string> IncludedStatuses { get; set; } = new();

    /// <summary>
    /// Validates the Notion API settings.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return false;

        if (string.IsNullOrWhiteSpace(BaseUrl))
            return false;

        if (RequestTimeoutSeconds < 5 || RequestTimeoutSeconds > 300)
            return false;

        if (MaxRetries < 0 || MaxRetries > 10)
            return false;

        if (RetryDelayMs < 100 || RetryDelayMs > 60000)
            return false;

        if (DefaultPageSize < 1 || DefaultPageSize > MaxPageSize)
            return false;

        if (CacheDurationMinutes < 0 || CacheDurationMinutes > 1440)
            return false;

        return true;
    }

    /// <summary>
    /// Returns a summary of the Notion API settings (without exposing the API key).
    /// </summary>
    public override string ToString()
    {
        var keyPreview = string.IsNullOrEmpty(ApiKey) ? "NOT SET" : $"{ApiKey.Substring(0, 8)}...";
        return $"NotionApiSettings(BaseUrl={BaseUrl}, ApiKey={keyPreview}, " +
               $"Timeout={RequestTimeoutSeconds}s, Databases={DatabaseIds.Count})";
    }

    /// <summary>
    /// Gets the API key masked for display in logs.
    /// </summary>
    public string GetMaskedApiKey()
    {
        if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 8)
            return "***";

        return ApiKey.Substring(0, 4) + "***" + ApiKey.Substring(ApiKey.Length - 4);
    }
}
