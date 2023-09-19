#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Services;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using DomainTask = NotionTaskSync.Domain.Models.Task;

/// <summary>
/// Extension methods for <see cref="NotionApiServiceTests"/> to provide reusable test utilities
/// for testing Notion API service functionality.
/// </summary>
public static class NotionApiServiceTestsExtensions
{
    /// <summary>
    /// Creates a mock HTTP response with the specified status code and content.
    /// </summary>
    /// <param name="statusCode">HTTP status code to return</param>
    /// <param name="content">Response content as string</param>
    /// <returns>Configured HttpResponseMessage</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/></exception>
    public static HttpResponseMessage CreateMockResponse(this HttpStatusCode statusCode, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with the specified status code and deserializable object.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize</typeparam>
    /// <param name="statusCode">HTTP status code to return</param>
    /// <param name="data">Data object to serialize as JSON</param>
    /// <returns>Configured HttpResponseMessage with JSON content</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/></exception>
    public static HttpResponseMessage CreateMockJsonResponse<T>(this HttpStatusCode statusCode, T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var content = JsonSerializer.Serialize(data);
        return statusCode.CreateMockResponse(content);
    }

    /// <summary>
    /// Sets up the mock HTTP handler to return a successful empty response.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="responseContent">Optional custom response content</param>
    /// <exception cref="ArgumentNullException"><paramref name="mockHandler"/> is <see langword="null"/></exception>
    public static void SetupSuccessResponse(this Mock<HttpMessageHandler> mockHandler, string? responseContent = null)
    {
        ArgumentNullException.ThrowIfNull(mockHandler);

        var mockResponse = responseContent != null
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseContent) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"results\": [], \"has_more\": false}") };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);
    }

    /// <summary>
    /// Sets up the mock HTTP handler to throw a specific exception.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="exception">Exception to throw</param>
    /// <exception cref="ArgumentNullException"><paramref name="mockHandler"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static void SetupThrows(this Mock<HttpMessageHandler> mockHandler, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(mockHandler);
        ArgumentNullException.ThrowIfNull(exception);

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    /// <summary>
    /// Verifies that the SendAsync method was called with the expected request message.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="times">Number of expected calls</param>
    /// <param name="assertion">Optional custom assertion on the request message</param>
    /// <exception cref="ArgumentNullException"><paramref name="mockHandler"/> is <see langword="null"/></exception>
    public static void VerifySendAsync(
        this Mock<HttpMessageHandler> mockHandler,
        Times times,
        Action<HttpRequestMessage>? assertion = null)
    {
        ArgumentNullException.ThrowIfNull(mockHandler);

        mockHandler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        if (assertion != null)
        {
            throw new NotSupportedException("Custom assertion validation is not supported in this method. Use separate verification.");
        }
    }

    /// <summary>
    /// Creates a test task with default values.
    /// </summary>
    /// <param name="title">Task title</param>
    /// <returns>Configured DomainTask</returns>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is <see langword="null"/></exception>
    public static global::NotionTaskSync.Domain.Models.Task CreateTestTask(this string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return new global::NotionTaskSync.Domain.Models.Task
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test task with default values.
    /// </summary>
    /// <returns>Configured DomainTask</returns>
    public static global::NotionTaskSync.Domain.Models.Task CreateTestTask()
    {
        return CreateTestTask("Test Task");
    }

    /// <summary>
    /// Asserts that a ValidationException was thrown with the expected parameter name.
    /// </summary>
    /// <param name="action">Action that should throw</param>
    /// <param name="expectedParamName">Expected parameter name in exception</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="expectedParamName"/> is <see langword="null"/></exception>
    public static async System.Threading.Tasks.Task AssertValidationExceptionAsync(
        this Func<System.Threading.Tasks.Task> action,
        string expectedParamName)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(expectedParamName);

        var exception = await global::Xunit.Assert.ThrowsAsync<global::NotionTaskSync.Domain.Exceptions.ValidationException>(action);
        exception.Message.Should().Contain(expectedParamName);
    }

    /// <summary>
    /// Notion API pagination response structure.
    /// </summary>
    private sealed record NotionPaginationResponse(
        List<object> results,
        bool has_more);

    /// <summary>
    /// Creates a mock response with pagination data.
    /// </summary>
    /// <param name="results">Number of results to include</param>
    /// <param name="hasMore">Whether there are more pages</param>
    /// <returns>Configured HttpResponseMessage with pagination data</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="results"/> is negative</exception>
    public static HttpResponseMessage CreatePaginationResponse(this int results, bool hasMore = false)
    {
        if (results < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(results), "Results count cannot be negative");
        }

        var responseData = new NotionPaginationResponse(
            Enumerable.Range(0, results)
                .Select(i => new { id = Guid.NewGuid().ToString() })
                .Cast<object>()
                .ToList(),
            hasMore
        );

        return HttpStatusCode.OK.CreateMockJsonResponse(responseData);
    }

    /// <summary>
    /// Sets up the mock to simulate multiple pages of results.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="totalPages">Total pages to simulate</param>
    /// <param name="itemsPerPage">Items per page</param>
    /// <exception cref="ArgumentNullException"><paramref name="mockHandler"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalPages"/> or <paramref name="itemsPerPage"/> is not positive</exception>
    public static void SetupMultiPageResponse(
        this Mock<HttpMessageHandler> mockHandler,
        int totalPages,
        int itemsPerPage = 100)
    {
        ArgumentNullException.ThrowIfNull(mockHandler);
        if (totalPages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPages), "Total pages must be positive");
        }
        if (itemsPerPage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemsPerPage), "Items per page must be positive");
        }

        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < totalPages; i++)
        {
            bool hasMore = i < totalPages - 1;
            responses.Add((itemsPerPage * (i + 1)).CreatePaginationResponse(hasMore));
        }

        int callCount = 0;
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                if (callCount < responses.Count)
                {
                    return responses[callCount++];
                }
                return HttpStatusCode.OK.CreateMockResponse("{\"results\": [], \"has_more\": false}");
            });
    }
}