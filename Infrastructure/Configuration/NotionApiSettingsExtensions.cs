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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool HasValidApiKey(this NotionApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return !string.IsNullOrWhiteSpace(settings.ApiKey);
    }

    /// <summary>
    /// Gets the effective base URL for API requests, ensuring it ends with a trailing slash.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The base URL with a trailing slash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string GetNormalizedBaseUrl(this NotionApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.BaseUrl.TrimEnd('/') + '/';
    }

    /// <summary>
    /// Gets the effective API version to use in requests.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The API version string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string GetEffectiveApiVersion(this NotionApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.ApiVersion ?? "2022-06-28";
    }

    /// <summary>
    /// Gets whether rate limiting should be respected based on configuration.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>True if rate limiting should be respected; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool ShouldRespectRateLimits(this NotionApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.RespectRateLimits;
    }

    /// <summary>
    /// Gets the effective page size for queries, bounded by the maximum allowed.
    /// </summary>
    /// <param name="settings">The Notion API settings.</param>
    /// <returns>The effective page size.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static int GetEffectivePageSize(this NotionApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Math.Min(settings.DefaultPageSize, settings.MaxPageSize);
    }
}
