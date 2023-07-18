using System;
using System.Collections.Generic;
using System.Linq;

namespace NotionTaskSync.Infrastructure.Configuration;

/// <summary>
/// Extension methods for <see cref="NotionApiSettings"/> providing additional functionality.
/// </summary>
public static class NotionApiSettingsExtensions
{
    /// <summary>
    /// Determines whether the API key is configured and valid.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>True if the API key is set and not empty; otherwise, false.</returns>
    public static bool HasValidApiKey(this NotionApiSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings?.ApiKey);
    }

    /// <summary>
    /// Gets the effective base URL for API requests, ensuring it ends with a trailing slash.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The base URL with a trailing slash.</returns>
    public static string GetNormalizedBaseUrl(this NotionApiSettings settings)
    {
        if (settings?.BaseUrl == null)
            return "https://api.notion.com/v1/";

        return settings.BaseUrl.TrimEnd('/') + '/';
    }

    /// <summary>
    /// Gets the effective API version to use in requests.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The API version string.</returns>
    public static string GetEffectiveApiVersion(this NotionApiSettings settings)
    {
        return settings?.ApiVersion ?? "2022-06-28";
    }

    /// <summary>
    /// Gets whether rate limiting should be respected based on configuration.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>True if rate limiting should be respected; otherwise, false.</returns>
    public static bool ShouldRespectRateLimits(this NotionApiSettings settings)
    {
        return settings?.RespectRateLimits ?? true;
    }

    /// <summary>
    /// Gets the effective page size for queries, bounded by the maximum allowed.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The effective page size.</returns>
    public static int GetEffectivePageSize(this NotionApiSettings settings)
    {
        if (settings == null)
            return 100;

        return Math.Min(settings.DefaultPageSize, settings.MaxPageSize);
    }
}
