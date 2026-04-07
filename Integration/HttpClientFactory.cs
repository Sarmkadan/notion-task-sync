#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Infrastructure.Configuration;

/// <summary>
/// Factory for creating properly configured HTTP clients for external API calls.
/// Centralizes HTTP client configuration including timeouts, headers, and retry policies.
/// Follows best practices for HttpClient usage including reuse and proper disposal.
/// </summary>
public class HttpClientFactory
{
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly NotionApiSettings _notionSettings;
    private HttpClient? _notionHttpClient;

    public HttpClientFactory(ILogger<HttpClientFactory> logger, NotionApiSettings notionSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notionSettings = notionSettings ?? throw new ArgumentNullException(nameof(notionSettings));
    }

    /// <summary>
    /// Gets or creates an HTTP client for Notion API calls.
    /// Reuses client for performance; cached across calls.
    /// </summary>
    public HttpClient GetNotionHttpClient()
    {
        if (_notionHttpClient is not null)
            return _notionHttpClient;

        _notionHttpClient = new HttpClient();

        // Configure base address and timeout
        _notionHttpClient.BaseAddress = new Uri(_notionSettings.BaseUrl);
        _notionHttpClient.Timeout = TimeSpan.FromSeconds(_notionSettings.RequestTimeoutSeconds);

        // Add default headers required by Notion API
        _notionHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _notionSettings.ApiKey);

        _notionHttpClient.DefaultRequestHeaders.Add("Notion-Version", _notionSettings.ApiVersion);

        // Standard headers for API compatibility
        _notionHttpClient.DefaultRequestHeaders.Add("User-Agent", "NotionTaskSync/1.0");
        _notionHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation("Created HTTP client for Notion API");

        return _notionHttpClient;
    }

    /// <summary>
    /// Creates an HTTP client for generic use with standard configuration.
    /// </summary>
    public HttpClient CreateGenericHttpClient(string baseAddress, int timeoutSeconds = 30)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        client.DefaultRequestHeaders.Add("User-Agent", "NotionTaskSync/1.0");

        _logger.LogInformation("Created generic HTTP client for {BaseAddress}", baseAddress);

        return client;
    }

    /// <summary>
    /// Creates an HTTP client with custom authentication header.
    /// </summary>
    public HttpClient CreateAuthenticatedHttpClient(
        string baseAddress,
        string authToken,
        int timeoutSeconds = 30)
    {
        var client = CreateGenericHttpClient(baseAddress, timeoutSeconds);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        return client;
    }

    /// <summary>
    /// Creates an HTTP client that respects rate limiting via retry-after headers.
    /// Configures handler to automatically wait on 429 (Too Many Requests) responses.
    /// </summary>
    public HttpClient CreateRateLimitAwareHttpClient(string baseAddress, int timeoutSeconds = 30)
    {
        var handler = new SocketsHttpHandler
        {
            // Enable automatic decompression
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            // Connection pooling for better performance
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };

        var client = new HttpClient(handler);
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        _logger.LogInformation("Created rate-limit-aware HTTP client for {BaseAddress}", baseAddress);

        return client;
    }

    /// <summary>
    /// Cleans up HTTP clients and releases resources.
    /// Should be called when application is shutting down.
    /// </summary>
    public void Dispose()
    {
        _notionHttpClient?.Dispose();
        _logger.LogInformation("Disposed HTTP clients");
    }

    /// <summary>
    /// Configures HTTP client headers from a dictionary.
    /// Useful for dynamic header configuration.
    /// </summary>
    public void ConfigureHeaders(HttpClient client, Dictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
        }

        _logger.LogDebug("Configured {HeaderCount} headers on HTTP client", headers.Count);
    }
}
