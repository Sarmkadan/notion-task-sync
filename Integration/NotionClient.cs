#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Low-level HTTP client wrapper for the Notion REST API.
/// Handles authentication headers, rate-limit retries, pagination cursors,
/// and JSON serialization. Used by <see cref="NotionTaskSync.Services.NotionApiService"/>
/// as the transport layer.
/// </summary>
public sealed class NotionClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotionClient> _logger;
    private readonly bool _ownsHttpClient;

    private const string BaseUrl = "https://api.notion.com/v1";
    private const string ApiVersion = "2022-06-28";
    private const int MaxRetries = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Initializes a new <see cref="NotionClient"/> with the given API key.
    /// </summary>
    /// <param name="apiKey">Notion integration token (starts with <c>ntn_</c> or <c>secret_</c>).</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClient">Optional pre-configured HTTP client; when <see langword="null"/> a new one is created.</param>
    public NotionClient(string apiKey, ILogger<NotionClient> logger, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key is required.", nameof(apiKey));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Add("Notion-Version", ApiVersion);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Queries a Notion database, returning all pages across all pagination cursors.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="filter">Optional JSON filter object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of raw page dictionaries.</returns>
    public async Task<List<Dictionary<string, object?>>> QueryDatabaseAsync(
        string databaseId,
        JsonElement? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        var allResults = new List<Dictionary<string, object?>>();
        string? startCursor = null;
        bool hasMore = true;

        while (hasMore)
        {
            var body = new Dictionary<string, object?>();
            if (filter.HasValue) body["filter"] = filter.Value;
            if (startCursor is not null) body["start_cursor"] = startCursor;

            var response = await SendWithRetryAsync(
                HttpMethod.Post,
                $"{BaseUrl}/databases/{databaseId}/query",
                body,
                cancellationToken);

            if (response.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(item.GetRawText());
                    if (dict is not null) allResults.Add(dict);
                }
            }

            hasMore = response.TryGetProperty("has_more", out var hm) && hm.GetBoolean();
            startCursor = hasMore && response.TryGetProperty("next_cursor", out var nc)
                ? nc.GetString()
                : null;
        }

        _logger.LogDebug("Queried database {DatabaseId}: {Count} pages total", databaseId, allResults.Count);
        return allResults;
    }

    /// <summary>
    /// Retrieves a single page by its identifier.
    /// </summary>
    /// <param name="pageId">The Notion page identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw page dictionary.</returns>
    public async Task<JsonElement> GetPageAsync(string pageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
        return await SendWithRetryAsync(HttpMethod.Get, $"{BaseUrl}/pages/{pageId}", null, cancellationToken);
    }

    /// <summary>
    /// Updates properties on an existing Notion page.
    /// </summary>
    /// <param name="pageId">The page to update.</param>
    /// <param name="properties">Property update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated page response.</returns>
    public async Task<JsonElement> UpdatePageAsync(
        string pageId,
        Dictionary<string, object> properties,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
        ArgumentNullException.ThrowIfNull(properties);

        var body = new Dictionary<string, object> { ["properties"] = properties };
        return await SendWithRetryAsync(HttpMethod.Patch, $"{BaseUrl}/pages/{pageId}", body, cancellationToken);
    }

    /// <summary>
    /// Creates a new page in a Notion database.
    /// </summary>
    /// <param name="databaseId">Target database.</param>
    /// <param name="properties">Page properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created page response.</returns>
    public async Task<JsonElement> CreatePageAsync(
        string databaseId,
        Dictionary<string, object> properties,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(properties);

        var body = new Dictionary<string, object>
        {
            ["parent"] = new Dictionary<string, string> { ["database_id"] = databaseId },
            ["properties"] = properties,
        };

        return await SendWithRetryAsync(HttpMethod.Post, $"{BaseUrl}/pages", body, cancellationToken);
    }

    private async Task<JsonElement> SendWithRetryAsync(
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonDocument.Parse(content).RootElement;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? DefaultRetryDelay * (attempt + 1);
                _logger.LogWarning("Rate limited by Notion API; retrying in {Delay}s (attempt {Attempt}/{Max})",
                    retryAfter.TotalSeconds, attempt + 1, MaxRetries);
                await System.Threading.Tasks.Task.Delay(retryAfter, cancellationToken);
                continue;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Notion API error {StatusCode}: {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Notion API returned {(int)response.StatusCode}: {errorContent}");
        }

        throw new HttpRequestException("Notion API request failed after maximum retries.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
