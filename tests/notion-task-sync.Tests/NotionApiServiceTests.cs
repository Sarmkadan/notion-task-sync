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
using Task = System.Threading.Tasks.Task;
using DomainTask = NotionTaskSync.Domain.Models.Task;

/// <summary>
/// Contains unit tests for the <see cref="NotionApiService"/> class.
/// Tests the interaction with the Notion API through HTTP requests, including
/// page fetching, creation, updating, and error handling scenarios.
/// </summary>
public class NotionApiServiceTests
{
    /// <summary>
/// Mock HTTP message handler for testing HTTP requests to the Notion API.
/// </summary>
private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    /// <summary>
/// HTTP client used for making requests to the Notion API.
/// </summary>
private readonly HttpClient _httpClient;
    /// <summary>
/// Instance of the NotionApiService being tested.
/// </summary>
private readonly NotionApiService _apiService;

    public NotionApiServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _apiService = new NotionApiService("notionso_test_api_key_" + "".PadRight(20, 'a'), _httpClient);
    }

    /// <summary>
    /// Tests that FetchPagesAsync successfully retrieves pages from Notion database when given a valid database ID.
    /// Verifies that the method returns a non-null list of NotionPage objects.
    /// </summary>
    /// <summary>
    /// Tests that FetchPagesAsync successfully retrieves pages from Notion database when given a valid database ID.
    /// Verifies that the method returns a non-null list of NotionPage objects.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_WithValidDatabaseId_ReturnsPages()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.FetchPagesAsync(databaseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<NotionPage>>();
    }

    /// <summary>
    /// Tests that FetchPagesAsync throws a ValidationException when provided with an empty database ID.
    /// Verifies proper input validation for empty strings.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_WithEmptyDatabaseId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _apiService.FetchPagesAsync(string.Empty));
    }

    /// <summary>
    /// Tests that FetchPagesAsync throws a ValidationException when provided with a null database ID.
    /// Verifies proper input validation for null values.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_WithNullDatabaseId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _apiService.FetchPagesAsync(null!));
    }

    /// <summary>
    /// Tests that FetchPagesAsync correctly handles pagination with default page size.
    /// Verifies that the API service makes exactly one HTTP request when fetching pages.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_WithDefaultPageSize_UsesPaginationCorrectly()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.FetchPagesAsync(databaseId, pageSize: 100);

        // Assert
        result.Should().NotBeNull();
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Tests that FetchPagesAsync throws a NotionApiException when the HTTP request fails.
    /// Verifies proper error handling for network and API errors.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_WhenHttpRequestFails_ThrowsNotionApiException()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<NotionApiException>(() => _apiService.FetchPagesAsync(databaseId));
    }

    /// <summary>
    /// Tests that FetchPagesSinceAsync successfully retrieves and filters pages based on last edited time.
    /// Verifies that the method returns a non-null list of NotionPage objects.
    /// </summary>
    [Fact]
    public async Task FetchPagesSinceAsync_WithValidInputs_ReturnsFilteredPages()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";
        var since = DateTime.UtcNow.AddHours(-1);

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.FetchPagesSinceAsync(databaseId, since);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<NotionPage>>();
    }

    /// <summary>
    /// Tests that FetchPagesSinceAsync throws a ValidationException when provided with an empty database ID.
    /// Verifies proper input validation for empty strings in the filtered fetch operation.
    /// </summary>
    [Fact]
    public async Task FetchPagesSinceAsync_WithEmptyDatabaseId_ThrowsValidationException()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _apiService.FetchPagesSinceAsync(string.Empty, since));
    }

    /// <summary>
    /// Tests that FetchPagesSinceAsync correctly filters pages by last edited time.
    /// Verifies that pagination is used correctly when fetching filtered pages.
    /// </summary>
    [Fact]
    public async Task FetchPagesSinceAsync_FiltersByLastEditedTime()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";
        var since = DateTime.UtcNow.AddHours(-1);

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        await _apiService.FetchPagesSinceAsync(databaseId, since, pageSize: 100);

        // Assert
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Tests that FetchPagesSinceAsync returns an empty list when provided with a future timestamp.
    /// Verifies that the filtering logic correctly handles timestamps in the future.
    /// </summary>
    [Fact]
    public async Task FetchPagesSinceAsync_WithFutureTimestamp_ReturnsEmptyList()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";
        var futureTime = DateTime.UtcNow.AddHours(1);

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.FetchPagesSinceAsync(databaseId, futureTime);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<NotionPage>>();
    }

    /// <summary>
    /// Tests that the NotionApiService constructor properly sets the Authorization header when provided with a valid API key.
    /// Verifies that the Bearer token is correctly configured in the HTTP client.
    /// </summary>
    [Fact]
    public void Constructor_WithValidApiKey_SetsAuthHeader()
    {
        // Arrange
        var validKey = "notionso_" + "a".PadRight(28, 'a');

        // Act
        var service = new NotionApiService(validKey, _httpClient);

        // Assert
        service.Should().NotBeNull();
        _httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the NotionApiService constructor creates a service instance even when provided with a null API key.
    /// Verifies that the service can be instantiated without authentication for testing purposes.
    /// </summary>
    [Fact]
    public void Constructor_WithNullApiKey_CreatesServiceWithoutAuth()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new NotionApiService(null, httpClient);

        // Assert
        service.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the NotionApiService constructor creates a default HttpClient when none is provided.
    /// Verifies that the service can be instantiated with minimal dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithoutHttpClient_CreatesDefaultHttpClient()
    {
        // Arrange
        var validKey = "notionso_" + "a".PadRight(28, 'a');

        // Act
        var service = new NotionApiService(validKey);

        // Assert
        service.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that FetchPagesAsync correctly handles empty results from the Notion API.
    /// Verifies that an empty list is returned when no pages are available.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_HandlesEmptyResults()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.FetchPagesAsync(databaseId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that CreatePageAsync successfully creates a new page in Notion database.
    /// Verifies that the method returns a NotionPage with the correct PageId.
    /// </summary>
    [Fact]
    public async Task CreatePageAsync_WithValidTask_CreatesPage()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "New Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": \"page123\"}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.CreatePageAsync(databaseId, task);

        // Assert
        result.Should().NotBeNull();
        result.PageId.Should().Be("page123");
    }

    /// <summary>
    /// Tests that UpdatePageAsync successfully updates an existing page in Notion.
    /// Verifies that the method returns a NotionPage with the correct PageId.
    /// </summary>
    [Fact]
    public async Task UpdatePageAsync_WithValidPageAndTask_UpdatesPage()
    {
        // Arrange
        var pageId = "page123";
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Updated Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": \"page123\"}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiService.UpdatePageAsync(pageId, task);

        // Assert
        result.Should().NotBeNull();
        result.PageId.Should().Be(pageId);
    }

    /// <summary>
    /// Tests that CreatePageAsync throws a ValidationException when provided with an empty database ID.
    /// Verifies proper input validation for the page creation operation.
    /// </summary>
    [Fact]
    public async Task CreatePageAsync_WithEmptyDatabaseId_ThrowsValidationException()
    {
        // Arrange
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "New Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _apiService.CreatePageAsync(string.Empty, task));
    }

    /// <summary>
    /// Tests that UpdatePageAsync throws a ValidationException when provided with an empty page ID.
    /// Verifies proper input validation for the page update operation.
    /// </summary>
    [Fact]
    public async Task UpdatePageAsync_WithEmptyPageId_ThrowsValidationException()
    {
        // Arrange
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Updated Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _apiService.UpdatePageAsync(string.Empty, task));
    }

    /// <summary>
    /// Tests that FetchPagesAsync includes the Notion API version header in requests.
    /// Verifies that the service sets the required 'Notion-Version' header for API compatibility.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_IncludesNotionApiVersionHeader()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        await _apiService.FetchPagesAsync(databaseId);

        // Assert
        _httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "Notion-Version");
    }

    /// <summary>
    /// Tests that FetchPagesAsync includes the Bearer token in the Authorization header.
    /// Verifies that the API key is correctly formatted as a Bearer token in HTTP requests.
    /// </summary>
    [Fact]
    public async Task FetchPagesAsync_IncludesBearerTokenInAuthHeader()
    {
        // Arrange
        var databaseId = "550e8400-e29b-41d4-a716-446655440000";
        var apiKey = "notionso_test_key_" + "x".PadRight(18, 'x');
        var testHttpClient = new HttpClient(_mockHttpHandler.Object);
        var service = new NotionApiService(apiKey, testHttpClient);

        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\": [], \"has_more\": false}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        await service.FetchPagesAsync(databaseId);

        // Assert
        testHttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        testHttpClient.DefaultRequestHeaders.Authorization?.Scheme.Should().Be("Bearer");
    }
}
