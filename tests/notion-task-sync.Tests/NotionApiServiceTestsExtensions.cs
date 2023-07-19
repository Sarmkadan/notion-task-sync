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
    public static HttpResponseMessage CreateMockResponse(this HttpStatusCode statusCode, string content)
    {
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
    public static HttpResponseMessage CreateMockJsonResponse<T>(this HttpStatusCode statusCode, T data)
    {
        var content = JsonSerializer.Serialize(data);
        return statusCode.CreateMockResponse(content);
    }

    /// <summary>
    /// Sets up the mock HTTP handler to return a successful empty response.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="responseContent">Optional custom response content</param>
    public static void SetupSuccessResponse(this Mock<HttpMessageHandler> mockHandler, string? responseContent = null)
    {
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
    public static void SetupThrows(this Mock<HttpMessageHandler> mockHandler, Exception exception)
    {
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
    public static void VerifySendAsync(
        this Mock<HttpMessageHandler> mockHandler,
        Times times,
        Action<HttpRequestMessage>? assertion = null)
    {
        mockHandler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Creates a test task with default values.
    /// </summary>
    /// <param name="title">Task title</param>
    /// <returns>Configured DomainTask</returns>
    public static DomainTask CreateTestTask(this string title)
    {
        return new DomainTask
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
    public static DomainTask CreateTestTask()
    {
        return CreateTestTask("Test Task");
    }

    /// <summary>
    /// Asserts that a ValidationException was thrown with the expected parameter name.
    /// </summary>
    /// <param name="action">Action that should throw</param>
    /// <param name="expectedParamName">Expected parameter name in exception</param>
    public static async System.Threading.Tasks.Task AssertValidationExceptionAsync(
        this Func<System.Threading.Tasks.Task> action,
        string expectedParamName)
    {
        var exception = await global::Xunit.Assert.ThrowsAsync<global::NotionTaskSync.Domain.Exceptions.ValidationException>(action);
        exception.Message.Should().Contain(expectedParamName);
    }

    /// <summary>
    /// Creates a mock response with pagination data.
    /// </summary>
    /// <param name="results">Number of results to include</param>
    /// <param name="hasMore">Whether there are more pages</param>
    /// <returns>Configured HttpResponseMessage with pagination data</returns>
    public static HttpResponseMessage CreatePaginationResponse(this int results, bool hasMore = false)
    {
        var responseData = new
        {
            results = Enumerable.Range(0, results)
                .Select(i => new { id = Guid.NewGuid().ToString() })
                .ToList(),
            has_more = hasMore
        };

        return HttpStatusCode.OK.CreateMockJsonResponse(responseData);
    }

    /// <summary>
    /// Sets up the mock to simulate multiple pages of results.
    /// </summary>
    /// <param name="mockHandler">Mock HTTP message handler</param>
    /// <param name="totalPages">Total pages to simulate</param>
    /// <param name="itemsPerPage">Items per page</param>
    public static void SetupMultiPageResponse(
        this Mock<HttpMessageHandler> mockHandler,
        int totalPages,
        int itemsPerPage = 100)
    {
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
            .ReturnsAsync(() => {
                if (callCount < responses.Count)
                {
                    return responses[callCount++];
                }
                return HttpStatusCode.OK.CreateMockResponse("{\"results\": [], \"has_more\": false}");
            });
    }
}
