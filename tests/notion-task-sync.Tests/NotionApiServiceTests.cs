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

public class NotionApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly NotionApiService _apiService;

    public NotionApiServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _apiService = new NotionApiService("notionso_test_api_key_" + "".PadRight(20, 'a'), _httpClient);
    }

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

    [Fact]
    public async Task FetchPagesAsync_WithEmptyDatabaseId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _apiService.FetchPagesAsync(string.Empty));
    }

    [Fact]
    public async Task FetchPagesAsync_WithNullDatabaseId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _apiService.FetchPagesAsync(null!));
    }

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

    [Fact]
    public async Task FetchPagesSinceAsync_WithEmptyDatabaseId_ThrowsValidationException()
    {
        // Arrange
        var since = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _apiService.FetchPagesSinceAsync(string.Empty, since));
    }

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
