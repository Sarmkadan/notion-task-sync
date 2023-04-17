// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotionTaskSync.Utils;

/// <summary>
/// Wrapper for making HTTP requests to external APIs with built-in error handling and retry logic.
/// Provides a simplified interface for API calls with automatic retry on transient failures.
/// Handles common scenarios like rate limiting, timeouts, and connection issues.
/// </summary>
public class ExternalApiWrapper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiWrapper> _logger;
    private readonly RetryHelper _retryHelper;

    public ExternalApiWrapper(
        HttpClient httpClient,
        ILogger<ExternalApiWrapper> logger,
        RetryHelper retryHelper)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryHelper = retryHelper ?? throw new ArgumentNullException(nameof(retryHelper));
    }

    /// <summary>
    /// Makes a GET request to the specified endpoint.
    /// Returns the response body as a string.
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _retryHelper.ExecuteWithRetryAsync(
                async () =>
                {
                    _logger.LogDebug("GET request to {Endpoint}", endpoint);
                    var result = await _httpClient.GetAsync(endpoint);
                    result.EnsureSuccessStatusCode();
                    return result;
                },
                IsTransientError);

            var content = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<T>(content);

            _logger.LogDebug("GET request successful: {Endpoint}", endpoint);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Makes a POST request with JSON body to the specified endpoint.
    /// </summary>
    public async Task<T?> PostAsync<T>(string endpoint, object payload)
    {
        try
        {
            var jsonContent = JsonConvert.SerializeObject(payload);

            var response = await _retryHelper.ExecuteWithRetryAsync(
                async () =>
                {
                    _logger.LogDebug("POST request to {Endpoint}", endpoint);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var result = await _httpClient.PostAsync(endpoint, content);
                    result.EnsureSuccessStatusCode();
                    return result;
                },
                IsTransientError);

            var responseContent = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<T>(responseContent);

            _logger.LogDebug("POST request successful: {Endpoint}", endpoint);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Makes a PUT request with JSON body to update a resource.
    /// </summary>
    public async Task<T?> PutAsync<T>(string endpoint, object payload)
    {
        try
        {
            var jsonContent = JsonConvert.SerializeObject(payload);

            var response = await _retryHelper.ExecuteWithRetryAsync(
                async () =>
                {
                    _logger.LogDebug("PUT request to {Endpoint}", endpoint);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    var result = await _httpClient.PutAsync(endpoint, content);
                    result.EnsureSuccessStatusCode();
                    return result;
                },
                IsTransientError);

            var responseContent = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<T>(responseContent);

            _logger.LogDebug("PUT request successful: {Endpoint}", endpoint);
            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Makes a DELETE request to remove a resource.
    /// </summary>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await _retryHelper.ExecuteWithRetryAsync(
                async () =>
                {
                    _logger.LogDebug("DELETE request to {Endpoint}", endpoint);
                    var result = await _httpClient.DeleteAsync(endpoint);
                    result.EnsureSuccessStatusCode();
                    return result;
                },
                IsTransientError);

            _logger.LogDebug("DELETE request successful: {Endpoint}", endpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE request failed: {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    private bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            HttpRequestException when ex.InnerException is TimeoutException => true,
            HttpRequestException when ex.Message.Contains("500") => true,
            HttpRequestException when ex.Message.Contains("503") => true,
            OperationCanceledException => true,
            _ => false
        };
    }
}
