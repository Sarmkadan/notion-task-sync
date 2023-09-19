#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for HttpClientFactory to provide additional convenience methods
/// for common HTTP client operations and patterns.
/// </summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Creates an HTTP client with custom base address and timeout, configured for JSON APIs.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="baseAddress">The base URL for the API</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30)</param>
    /// <returns>Configured HttpClient instance</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="baseAddress"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress"/> is empty or whitespace</exception>
    public static HttpClient CreateJsonApiClient(
        this HttpClientFactory factory,
        string baseAddress,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

        var client = factory.CreateGenericHttpClient(baseAddress, timeoutSeconds);

        // Configure for JSON API usage
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("User-Agent", "NotionTaskSync/1.0");

        return client;
    }

    /// <summary>
    /// Creates an HTTP client with custom authentication and JSON API configuration.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="baseAddress">The base URL for the API</param>
    /// <param name="authToken">Bearer token for authentication</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30)</param>
    /// <returns>Configured HttpClient instance with authentication</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/>, <paramref name="baseAddress"/>, or <paramref name="authToken"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress"/> or <paramref name="authToken"/> is empty or whitespace</exception>
    public static HttpClient CreateAuthenticatedJsonApiClient(
        this HttpClientFactory factory,
        string baseAddress,
        string authToken,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);

        var client = factory.CreateAuthenticatedHttpClient(baseAddress, authToken, timeoutSeconds);

        // Configure for JSON API usage
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    /// <summary>
    /// Creates an HTTP client with custom headers for specific API requirements.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="baseAddress">The base URL for the API</param>
    /// <param name="headers">Dictionary of headers to add</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30)</param>
    /// <returns>Configured HttpClient instance with custom headers</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/>, <paramref name="baseAddress"/>, or <paramref name="headers"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress"/> is empty or whitespace</exception>
    public static HttpClient CreateCustomHeadersClient(
        this HttpClientFactory factory,
        string baseAddress,
        Dictionary<string, string> headers,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);
        ArgumentNullException.ThrowIfNull(headers);

        var client = factory.CreateGenericHttpClient(baseAddress, timeoutSeconds);

        // Apply custom headers
        foreach (var kvp in headers)
        {
            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
        }

        return client;
    }

    /// <summary>
    /// Creates an HTTP client configured for Notion API with custom version header.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="notionApiVersion">Notion API version to use</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30)</param>
    /// <returns>Configured HttpClient instance for Notion API</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="notionApiVersion"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="notionApiVersion"/> is empty or whitespace</exception>
    public static HttpClient CreateNotionClientWithVersion(
        this HttpClientFactory factory,
        string notionApiVersion,
        int timeoutSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(notionApiVersion);

        var notionClient = factory.GetNotionHttpClient();
        var client = new HttpClient();
        client.BaseAddress = new Uri(notionClient.BaseAddress?.ToString() ?? "https://api.notion.com/");
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        // Configure Notion-specific headers
        if (notionClient.DefaultRequestHeaders.Authorization is not null)
        {
            client.DefaultRequestHeaders.Authorization = notionClient.DefaultRequestHeaders.Authorization;
        }

        client.DefaultRequestHeaders.Add("Notion-Version", notionApiVersion);
        client.DefaultRequestHeaders.Add("User-Agent", "NotionTaskSync/1.0");
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    /// <summary>
    /// Executes a GET request and returns the response as a string.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="client">The HttpClient to use</param>
    /// <param name="requestUri">The request URI</param>
    /// <returns>Response content as string</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/>, <paramref name="client"/>, or <paramref name="requestUri"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="requestUri"/> is empty or whitespace</exception>
    /// <exception cref="HttpRequestException">Request failed or returned non-success status code</exception>
    public static async Task<string> GetStringAsync(
        this HttpClientFactory factory,
        HttpClient client,
        string requestUri)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Executes a POST request with JSON content and returns the response.
    /// </summary>
    /// <param name="factory">The HttpClientFactory instance</param>
    /// <param name="client">The HttpClient to use</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <returns>Response content as string</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/>, <paramref name="client"/>, <paramref name="requestUri"/>, or <paramref name="content"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="requestUri"/> is empty or whitespace</exception>
    /// <exception cref="HttpRequestException">Request failed or returned non-success status code</exception>
    public static async Task<string> PostJsonAsync(
        this HttpClientFactory factory,
        HttpClient client,
        string requestUri,
        HttpContent content)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        ArgumentNullException.ThrowIfNull(content);

        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}