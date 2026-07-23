#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides integration with the Notion API for reading and writing task data.
/// Handles authentication, pagination, error handling, and data transformation.
/// </summary>
public class NotionApiService
{
    internal readonly string? _apiKey;
    internal readonly HttpClient _httpClient;
    internal readonly List<string> _includedStatuses;
    internal readonly AppJsonSerializerContext _jsonContext;
    internal const string NotionApiBaseUrl = "https://api.notion.com/v1";
    internal const string NotionApiVersion = "2022-06-28";
    private readonly SemaphoreSlim _concurrencySemaphore = new SemaphoreSlim(4);

    public NotionApiService(string? apiKey)
    : this(apiKey, null, new List<string>())
    {
    }

    public NotionApiService(string? apiKey, HttpClient? httpClient)
    : this(apiKey, httpClient, new List<string>())
    {
    }

    public NotionApiService(string? apiKey, HttpClient? httpClient, List<string> includedStatuses)
    {
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
        _includedStatuses = includedStatuses ?? new List<string>();
        _jsonContext = new AppJsonSerializerContext();

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            _httpClient.DefaultRequestHeaders.Remove("Notion-Version");
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", NotionApiVersion);
        }
    }

    /// <summary>
    /// Fetches all pages from a Notion database with automatic cursor-based pagination.
    /// Continues fetching until all pages are retrieved or an error occurs.
    /// Uses concurrent fetching with bounded SemaphoreSlim(4) for improved performance.
    /// </summary>
    /// <param name="databaseId">The Notion database UUID to query.</param>
    /// <param name="pageSize">Number of results per API call (max 100). Defaults to 100.</param>
    /// <returns>A complete list of <see cref="NotionPage"/> entities from the database. For large databases, consider using <see cref="FetchPagesSinceAsync"/> for better performance.</returns>
    /// <exception cref="ValidationException">Thrown when <paramref name="databaseId"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is not between 1 and 100.</exception>
/// <exception cref="NotionApiException">Thrown when the Notion API request fails.</exception>
    public virtual async Task<List<NotionPage>> FetchPagesAsync(string databaseId, int pageSize = 100)
    {
        if (string.IsNullOrEmpty(databaseId))
            throw new ValidationException("Database ID cannot be empty");

    if (pageSize < 1 || pageSize > 100)
        throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be between 1 and 100");

        var pages = new List<NotionPage>();
        var startCursor = string.Empty;
        var hasMore = true;

        try
        {
            while (hasMore)
            {
                var url = $"{NotionApiBaseUrl}/databases/{databaseId}/query";
                var payload = new
                {
                    page_size = pageSize,
                    start_cursor = string.IsNullOrEmpty(startCursor) ? null : startCursor,
                    filter = CreateStatusFilter()
                };

                // Wait for semaphore slot to become available (bounded concurrency)
                await _concurrencySemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var response = await PostAsync(url, payload).ConfigureAwait(false);

                    if (response is null)
                    {
                        hasMore = false;
                        continue;
                    }

                    using var document = JsonDocument.Parse(response);
                    var root = document.RootElement;

                    if (root.TryGetProperty("results", out var results))
                    {
                        foreach (var result in results.EnumerateArray())
                        {
                            pages.Add(ParseNotionPage(result, databaseId));
                        }
                    }

                    hasMore = root.TryGetProperty("has_more", out var hasMoreElement) && hasMoreElement.GetBoolean();
                    startCursor = hasMore && root.TryGetProperty("next_cursor", out var cursorElement) && cursorElement.ValueKind == JsonValueKind.String
                        ? cursorElement.GetString() ?? string.Empty
                        : string.Empty;
                }
                finally
                {
                    // Release semaphore when batch is complete
                    _concurrencySemaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to fetch pages from database {databaseId}: {ex.Message}", ex);
        }

        return pages;
    }

    /// <summary>
    /// Fetches only pages that have been edited on or after <paramref name="since"/> (incremental sync).
    /// Uses Notion's <c>filter</c> parameter with <c>last_edited_time</c> and cursor-based
    /// pagination so that large databases are queried efficiently — only changed entries are
    /// transferred, dramatically reducing API calls and sync duration.
    /// </summary>
    /// <param name="databaseId">The Notion database UUID to query.</param>
    /// <param name="since">Only pages edited on or after this timestamp are returned. Uses UTC timezone.</param>
    /// <param name="pageSize">Results per API call (max 100). Defaults to 100.</param>
    /// <returns>Pages whose <c>last_edited_time</c> is on or after <paramref name="since"/>.</returns>
    /// <exception cref="ValidationException">Thrown when <paramref name="databaseId"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is not between 1 and 100.</exception>
/// <exception cref="NotionApiException">Thrown when the Notion API request fails.</exception>
    public virtual async Task<List<NotionPage>> FetchPagesSinceAsync(
        string databaseId,
        DateTime since,
        int pageSize = 100)
    {
        if (string.IsNullOrEmpty(databaseId))
            throw new ValidationException("Database ID cannot be empty");

    if (pageSize < 1 || pageSize > 100)
        throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be between 1 and 100");

        var pages = new List<NotionPage>();
        var startCursor = string.Empty;
        var hasMore = true;

        try
        {
            while (hasMore)
            {
                var url = $"{NotionApiBaseUrl}/databases/{databaseId}/query";

                // Notion's filter API supports timestamp-based filtering on last_edited_time.
                // Combined with start_cursor this gives full cursor-based incremental pagination.
                var payload = new
                {
                    page_size = pageSize,
                    start_cursor = string.IsNullOrEmpty(startCursor) ? null : startCursor,
                    filter = CreateIncrementalFilter(since)
                };

                // Wait for semaphore slot to become available (bounded concurrency)
                await _concurrencySemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var response = await PostAsync(url, payload).ConfigureAwait(false);

                    if (response is null)
                    {
                        hasMore = false;
                        continue;
                    }

                    using var document = JsonDocument.Parse(response);
                    var root = document.RootElement;

                    if (root.TryGetProperty("results", out var results))
                    {
                        foreach (var result in results.EnumerateArray())
                        {
                            pages.Add(ParseNotionPage(result, databaseId));
                        }
                    }

                    hasMore = root.TryGetProperty("has_more", out var hasMoreElement) && hasMoreElement.GetBoolean();
                    startCursor = hasMore && root.TryGetProperty("next_cursor", out var cursorElement) && cursorElement.ValueKind == JsonValueKind.String
                        ? cursorElement.GetString() ?? string.Empty
                        : string.Empty;
                }
                finally
                {
                    // Release semaphore when batch is complete
                    _concurrencySemaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to fetch incremental pages from database {databaseId} since {since:O}: {ex.Message}", ex);
        }

        return pages;
    }

    /// <summary>
    /// Creates a filter object for status-based filtering when fetching all pages.
    /// Returns null if no status filtering is configured.
    /// </summary>
    private object? CreateStatusFilter()
    {
        if (_includedStatuses == null || _includedStatuses.Count == 0)
            return null;

        return new
        {
            and = new object[]
            {
                new
                {
                    property = "Status",
                    status = new
                    {
                        is_not_empty = true
                    }
                },
                new
                {
                    or = _includedStatuses.Select(status => new
                    {
                        property = "Status",
                        status = new
                        {
                            equals = status
                        }
                    }).ToArray()
                }
            }
        };
    }

    /// <summary>
    /// Creates a filter object for incremental sync with status filtering.
    /// </summary>
    private object CreateIncrementalFilter(DateTime since)
    {
        var baseFilter = new
        {
            timestamp = "last_edited_time",
            last_edited_time = new
            {
                on_or_after = since.ToUniversalTime().ToString("o")
            }
        };

        if (_includedStatuses == null || _includedStatuses.Count == 0)
            return baseFilter;

        return new
        {
            and = new object[]
            {
                baseFilter,
                new
                {
                    property = "Status",
                    status = new
                    {
                        is_not_empty = true
                    }
                },
                new
                {
                    or = _includedStatuses.Select(status => new
                    {
                        property = "Status",
                        status = new
                        {
                            equals = status
                        }
                    }).ToArray()
                }
            }
        };
    }

    /// <summary>
    /// Parses a raw Notion "page" JSON object into a <see cref="NotionPage"/>, extracting
    /// the title from the first title-typed property and copying the remaining properties
    /// into <see cref="NotionPage.Properties"/> as plain text/JSON fragments.
    /// </summary>
    private static NotionPage ParseNotionPage(JsonElement element, string fallbackDatabaseId)
    {
        var pageId = element.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;

        var databaseId = fallbackDatabaseId;
        if (element.TryGetProperty("parent", out var parentElement) &&
            parentElement.TryGetProperty("database_id", out var parentDbElement))
        {
            databaseId = parentDbElement.GetString() ?? fallbackDatabaseId;
        }

        var title = string.Empty;
        var properties = new Dictionary<string, object?>();

        if (element.TryGetProperty("properties", out var propertiesElement))
        {
            foreach (var property in propertiesElement.EnumerateObject())
            {
                var propertyValue = property.Value;

                if (propertyValue.TryGetProperty("title", out var titleArray) && titleArray.ValueKind == JsonValueKind.Array)
                {
                    var extractedTitle = string.Concat(
                        titleArray.EnumerateArray()
                            .Select(t => t.TryGetProperty("plain_text", out var plainText) ? plainText.GetString() : null));

                    if (!string.IsNullOrEmpty(extractedTitle) && string.IsNullOrEmpty(title))
                        title = extractedTitle;

                    properties[property.Name] = extractedTitle;
                }
                else if (propertyValue.TryGetProperty("rich_text", out var richTextArray) && richTextArray.ValueKind == JsonValueKind.Array)
                {
                    properties[property.Name] = string.Concat(
                        richTextArray.EnumerateArray()
                            .Select(t => t.TryGetProperty("plain_text", out var plainText) ? plainText.GetString() : null));
                }
                else if (propertyValue.TryGetProperty("select", out var selectElement) && selectElement.ValueKind == JsonValueKind.Object)
                {
                    properties[property.Name] = selectElement.TryGetProperty("name", out var selectName) ? selectName.GetString() : null;
                }
                else if (propertyValue.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Object)
                {
                    properties[property.Name] = statusElement.TryGetProperty("name", out var statusName) ? statusName.GetString() : null;
                }
                else
                {
                    properties[property.Name] = propertyValue.GetRawText();
                }
            }
        }

        if (string.IsNullOrEmpty(title))
            title = "Untitled";

        var page = new NotionPage(pageId, databaseId, title)
        {
            Properties = properties,
            Archived = element.TryGetProperty("archived", out var archivedElement) && archivedElement.GetBoolean(),
            Url = element.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null
        };

        if (element.TryGetProperty("created_time", out var createdTimeElement) &&
            DateTime.TryParse(createdTimeElement.GetString(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var createdTime))
        {
            page.CreatedTime = createdTime;
        }

        if (element.TryGetProperty("last_edited_time", out var lastEditedTimeElement) &&
            DateTime.TryParse(lastEditedTimeElement.GetString(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var lastEditedTime))
        {
            page.LastEditedTime = lastEditedTime;
        }

        if (element.TryGetProperty("created_by", out var createdByElement) && createdByElement.TryGetProperty("id", out var createdById))
        {
            page.CreatedBy = createdById.GetString();
        }

        if (element.TryGetProperty("last_edited_by", out var lastEditedByElement) && lastEditedByElement.TryGetProperty("id", out var lastEditedById))
        {
            page.LastEditedBy = lastEditedById.GetString();
        }

        return page;
    }

    /// <summary>
    /// Retrieves a single page from Notion by its ID.
    /// </summary>
    public virtual async Task<NotionPage> FetchPageAsync(string pageId)
    {
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID cannot be empty");

        try
        {
            var url = $"{NotionApiBaseUrl}/pages/{pageId}";
            var response = await GetAsync(url).ConfigureAwait(false);

            if (response is null)
                throw new NotionApiException($"Notion API returned no content for page {pageId}");

            using var document = JsonDocument.Parse(response);
            return ParseNotionPage(document.RootElement, string.Empty);
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to fetch page {pageId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a new page in a Notion database from a task.
    /// </summary>
    public virtual async Task<NotionPage> CreatePageAsync(string databaseId, global::NotionTaskSync.Domain.Models.Task task)
    {
        if (string.IsNullOrEmpty(databaseId))
            throw new ValidationException("Database ID cannot be empty");

        if (!task.Validate())
            throw new ValidationException("Invalid task cannot be created in Notion");

        try
        {
            var url = $"{NotionApiBaseUrl}/pages";
            var payload = new
            {
                parent = new { database_id = databaseId },
                properties = new
                {
                    Title = new { title = new[] { new { text = new { content = task.Title } } } },
                    Description = new { rich_text = new[] { new { text = new { content = task.Description ?? string.Empty } } } }
                }
            };

            var response = await PostAsync(url, payload).ConfigureAwait(false);

            if (response is null)
                throw new NotionApiException($"Notion API returned no content when creating a page for task {task.Id}");

            using var document = JsonDocument.Parse(response);
            return ParseNotionPage(document.RootElement, databaseId);
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to create page for task {task.Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates an existing page in Notion with task data.
    /// </summary>
    public virtual async Task<NotionPage> UpdatePageAsync(string pageId, global::NotionTaskSync.Domain.Models.Task task)
    {
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID cannot be empty");

        if (!task.Validate())
            throw new ValidationException("Invalid task data for update");

        try
        {
            var url = $"{NotionApiBaseUrl}/pages/{pageId}";
            var payload = new
            {
                properties = new
                {
                    Title = new { title = new[] { new { text = new { content = task.Title } } } },
                    Status = new { status = new { name = task.Status.ToString() } }
                }
            };

            var response = await PatchAsync(url, payload).ConfigureAwait(false);

            if (response is null)
                throw new NotionApiException($"Notion API returned no content when updating page {pageId}");

            using var document = JsonDocument.Parse(response);
            return ParseNotionPage(document.RootElement, string.Empty);
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to update page {pageId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes or archives a page in Notion.
    /// </summary>
    public virtual async System.Threading.Tasks.Task ArchivePageAsync(string pageId)
    {
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID cannot be empty");

        try
        {
            var url = $"{NotionApiBaseUrl}/pages/{pageId}";
            var payload = new { archived = true };

            await PatchAsync(url, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new NotionApiException(
                $"Failed to archive page {pageId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests the API connection by verifying the API key is valid.
    /// </summary>
    public virtual async Task<bool> TestConnectionAsync()
    {
        try
        {
            var url = $"{NotionApiBaseUrl}/users/me";
            var response = await GetAsync(url).ConfigureAwait(false);
            return response is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Makes a GET request to the Notion API.
    /// </summary>
    private async Task<string?> GetAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            throw new NotionApiException($"GET request failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Makes a POST request to the Notion API.
    /// </summary>
    private async Task<string?> PostAsync(string url, object payload)
    {
        try
        {
            var json = _jsonContext.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            throw new NotionApiException($"POST request failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Makes a PATCH request to the Notion API.
    /// </summary>
    private async Task<string?> PatchAsync(string url, object payload)
    {
        try
        {
            var json = _jsonContext.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            throw new NotionApiException($"PATCH request failed: {ex.Message}", ex);
        }
    }
}