#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Data.Mappers;
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Integration;
using NotionTaskSync.IntegrationTests.Support;
using Xunit;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// Integration tests for <see cref="NotionClient"/> over a real <see cref="HttpClient"/> pipeline,
/// with only the network transport replaced by <see cref="FakeNotionHttpHandler"/>.
/// Request bodies are checked as parsed JSON, not as strings.
/// </summary>
public sealed class NotionClientTests : IDisposable
{
    private const string ApiKey = "integration-test-token";
    private const string DatabaseId = "5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a10";
    private const string PageId = "3a7e1c2d-5b4f-4e8a-9c01-6f2d7b8e9a11";
    private const string ApiBase = "https://api.notion.com/v1";

    private readonly FakeNotionHttpHandler _notion = new();
    private readonly HttpClient _httpClient;
    private readonly NotionClient _client;

    /// <summary>
    /// Creates a client whose only transport is the scripted fake handler.
    /// </summary>
    public NotionClientTests()
    {
        _httpClient = new HttpClient(_notion);
        _client = new NotionClient(ApiKey, NullLogger<NotionClient>.Instance, _httpClient);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task QueryDatabaseAsync_FollowsCursorAndAggregatesAllPages()
    {
        _notion.EnqueueJson(HttpStatusCode.OK, QueryPageJson(hasMore: true, nextCursor: "cursor-page-2", "page-1", "page-2"));
        _notion.EnqueueJson(HttpStatusCode.OK, QueryPageJson(hasMore: false, nextCursor: null, "page-3"));

        var pages = await _client.QueryDatabaseAsync(DatabaseId);

        pages.Select(p => ((JsonElement)p["id"]!).GetString()).Should().Equal("page-1", "page-2", "page-3");

        _notion.Requests.Should().HaveCount(2);
        _notion.Requests.Should().OnlyContain(r =>
            r.Method == HttpMethod.Post && r.Uri == new Uri($"{ApiBase}/databases/{DatabaseId}/query"));

        var firstBody = JsonNode.Parse(_notion.Requests[0].Body)!;
        firstBody["start_cursor"].Should().BeNull("the first query has no cursor");

        var secondBody = JsonNode.Parse(_notion.Requests[1].Body)!;
        secondBody["start_cursor"]!.GetValue<string>().Should().Be("cursor-page-2");
    }

    [Fact]
    public async Task GetPageAsync_RateLimitedWithRetryAfter_RetriesAndSucceeds()
    {
        _notion.EnqueueJson(HttpStatusCode.TooManyRequests, "{\"object\":\"error\",\"status\":429,\"code\":\"rate_limited\"}", retryAfterSeconds: 1);
        _notion.EnqueueJson(HttpStatusCode.OK, PageJson(PageId));

        var stopwatch = Stopwatch.StartNew();
        var page = await _client.GetPageAsync(PageId);
        stopwatch.Stop();

        page.GetProperty("id").GetString().Should().Be(PageId);

        _notion.Requests.Should().HaveCount(2);
        _notion.Requests.Should().OnlyContain(r =>
            r.Method == HttpMethod.Get && r.Uri == new Uri($"{ApiBase}/pages/{PageId}"));

        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900),
            "the client must wait for the Retry-After delay before the second attempt");
    }

    [Fact]
    public async Task GetPageAsync_Unauthorized_ThrowsAuthenticationFailureWithoutRetrying()
    {
        _notion.EnqueueJson(HttpStatusCode.Unauthorized,
            "{\"object\":\"error\",\"status\":401,\"code\":\"unauthorized\",\"message\":\"API token is invalid.\"}");

        Func<Task> act = () => _client.GetPageAsync(PageId);

        var assertions = await act.Should().ThrowAsync<NotionApiException>();
        assertions.WithMessage("*authentication failed*401*");
        assertions.Which.HttpStatusCode.Should().Be(401);

        _notion.Requests.Should().HaveCount(1, "authentication errors are not retryable");
    }

    [Fact]
    public async Task GetPageAsync_SendsBearerTokenAndNotionVersionHeaders()
    {
        _notion.EnqueueJson(HttpStatusCode.OK, PageJson(PageId));

        await _client.GetPageAsync(PageId);

        var headers = _notion.Requests.Single().Headers;
        headers["Authorization"].Should().Equal($"Bearer {ApiKey}");
        headers["Notion-Version"].Should().Equal("2022-06-28");
        headers["Accept"].Should().Contain("application/json");
    }

    [Fact]
    public async Task UpdatePageAsync_SendsPropertyPayloadProducedByPropertyMapper()
    {
        var mapper = new PropertyMapper();
        var properties = new Dictionary<string, object>();

        var localProperties = new[]
        {
            LocalProperty("Name", "Ship the sync fix", PropertyDataType.String),
            LocalProperty("Done", "true", PropertyDataType.Boolean),
            LocalProperty("Estimate", "3.5", PropertyDataType.Decimal),
            LocalProperty("Due", "2026-10-30", PropertyDataType.DateTime),
        };

        foreach (var local in localProperties)
        {
            var mapped = mapper.MapToNotion(local);
            properties[mapped.Key] = mapped.Value;
        }

        _notion.EnqueueJson(HttpStatusCode.OK, PageJson(PageId));

        await _client.UpdatePageAsync(PageId, properties);

        var request = _notion.Requests.Single();
        request.Method.Should().Be(HttpMethod.Patch);
        request.Uri.Should().Be(new Uri($"{ApiBase}/pages/{PageId}"));
        request.Headers["Content-Type"].Should().Contain(value => value.StartsWith("application/json", StringComparison.Ordinal));

        var body = JsonNode.Parse(request.Body)!.AsObject();
        body.Select(p => p.Key).Should().Equal("properties");

        var sent = body["properties"]!;
        sent.AsObject().Select(p => p.Key).Should().BeEquivalentTo("Name", "Done", "Estimate", "Due");

        sent["Name"]!["rich_text"]![0]!["text"]!["content"]!.GetValue<string>().Should().Be("Ship the sync fix");
        sent["Done"]!["checkbox"]!.GetValue<bool>().Should().BeTrue();
        sent["Estimate"]!["number"]!.GetValue<double>().Should().Be(3.5);
        sent["Due"]!["date"]!["start"]!.GetValue<string>().Should().Be("2026-10-30");
    }

    // The TaskProperty constructor does not set its required members, so they are set in the initializer too.
    private static TaskProperty LocalProperty(string name, string value, PropertyDataType dataType) =>
        new(name, value, dataType) { PropertyName = name, TaskId = Guid.NewGuid() };

    private static string PageJson(string pageId) =>
        JsonSerializer.Serialize(new { @object = "page", id = pageId });

    private static string QueryPageJson(bool hasMore, string? nextCursor, params string[] pageIds) =>
        JsonSerializer.Serialize(new
        {
            @object = "list",
            results = pageIds.Select(id => new { @object = "page", id }),
            has_more = hasMore,
            next_cursor = nextCursor,
        });
}
