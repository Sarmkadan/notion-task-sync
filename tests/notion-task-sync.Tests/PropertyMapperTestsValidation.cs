#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Data.Mappers;
using NotionTaskSync.Domain.Exceptions;
using NotionTaskSync.Integration;
using Xunit;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// Tests for <see cref="PropertyMapper.ValidateAgainstDatabaseAsync"/>. A real <see cref="NotionClient"/>
/// is used, with only the transport replaced by <see cref="FakeDatabaseHandler"/>.
/// </summary>
public class PropertyMapperTestsValidation : IDisposable
{
    private const string DatabaseId = "5f1c0a2e-7b3d-4c8e-9a61-2d4b8e0f7a10";

    private readonly FakeDatabaseHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly NotionClient _client;
    private readonly PropertyMapper _mapper = new();

    /// <summary>
    /// Creates the client over the fake handler.
    /// </summary>
    public PropertyMapperTestsValidation()
    {
        _httpClient = new HttpClient(_handler);
        _client = new NotionClient("test-token", NullLogger<NotionClient>.Instance, _httpClient);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        _httpClient.Dispose();
    }

    private static PropertyMappingOptions Options(params (string Field, string NotionName)[] mappings)
    {
        var options = new PropertyMappingOptions();
        foreach (var (field, notionName) in mappings)
            options.Properties[field] = notionName;
        return options;
    }

    private static string Schema(params (string Name, string Type)[] properties)
    {
        var entries = new List<string>();
        foreach (var (name, type) in properties)
            entries.Add($"\"{name}\": {{ \"id\": \"x\", \"name\": \"{name}\", \"type\": \"{type}\" }}");
        return $"{{ \"object\": \"database\", \"id\": \"{DatabaseId}\", \"properties\": {{ {string.Join(", ", entries)} }} }}";
    }

    private static readonly string FullSchema = Schema(
        ("Name", "title"),
        ("Status", "status"),
        ("Priority", "select"),
        ("Due Date", "date"));

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_AllMappingsMatch_ReturnsNoProblems()
    {
        _handler.Respond(HttpStatusCode.OK, FullSchema);
        var options = Options(("Title", "Name"), ("Status", "Status"), ("Priority", "Priority"), ("DueDate", "Due Date"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_RequestsDatabaseEndpoint()
    {
        _handler.Respond(HttpStatusCode.OK, FullSchema);

        await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, Options(("Title", "Name"), ("Status", "Status")));

        _handler.LastRequest.Should().NotBeNull();
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        _handler.LastRequest.RequestUri!.AbsolutePath.Should().Be($"/v1/databases/{DatabaseId}");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_MappedPropertyMissing_IsReported()
    {
        _handler.Respond(HttpStatusCode.OK, FullSchema);
        var options = Options(("Title", "Name"), ("Status", "Status"), ("DueDate", "Deadline"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Mapped property 'Deadline' (field 'DueDate') does not exist in the Notion database.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_PropertyNameDiffersByCase_IsReportedAsMissing()
    {
        _handler.Respond(HttpStatusCode.OK, FullSchema);
        var options = Options(("Title", "name"), ("Status", "Status"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Mapped property 'name' (field 'Title') does not exist in the Notion database.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_TitleMappedToNonTitleType_IsReported()
    {
        _handler.Respond(HttpStatusCode.OK, Schema(("Notes", "rich_text"), ("Status", "status")));
        var options = Options(("Title", "Notes"), ("Status", "Status"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Mapped property 'Notes' (field 'Title') has Notion type 'rich_text', but 'title' is expected.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_StatusMappedToSelect_IsReported()
    {
        _handler.Respond(HttpStatusCode.OK, Schema(("Name", "title"), ("Status", "select")));
        var options = Options(("Title", "Name"), ("Status", "Status"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().ContainSingle()
            .Which.Should().Be("Mapped property 'Status' (field 'Status') has Notion type 'select', but 'status' is expected.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_FieldWithoutKnownType_OnlyChecksExistence()
    {
        // Priority has no fixed Notion type in the sync code, so any type on an existing property passes.
        _handler.Respond(HttpStatusCode.OK, Schema(("Name", "title"), ("Status", "status"), ("Priority", "number")));
        var options = Options(("Title", "Name"), ("Status", "Status"), ("Priority", "Priority"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_EmptyNotionNameIsSkipped_LeftToOfflineCheck()
    {
        _handler.Respond(HttpStatusCode.OK, FullSchema);
        var options = Options(("Title", "Name"), ("Status", "Status"), ("Priority", ""));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_ResponseWithoutProperties_ReturnsSingleProblem()
    {
        _handler.Respond(HttpStatusCode.OK, $"{{ \"object\": \"database\", \"id\": \"{DatabaseId}\" }}");

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, Options(("Title", "Name")));

        problems.Should().ContainSingle()
            .Which.Should().Be($"Notion database '{DatabaseId}' returned no property schema.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_MultipleProblems_AreAllReported()
    {
        _handler.Respond(HttpStatusCode.OK, Schema(("Name", "rich_text"), ("Status", "status")));
        var options = Options(("Title", "Name"), ("Status", "Status"), ("DueDate", "Due Date"));

        var problems = await _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, options);

        problems.Should().HaveCount(2);
        problems.Should().Contain("Mapped property 'Name' (field 'Title') has Notion type 'rich_text', but 'title' is expected.");
        problems.Should().Contain("Mapped property 'Due Date' (field 'DueDate') does not exist in the Notion database.");
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_NotionRejectsToken_PropagatesApiException()
    {
        _handler.Respond(HttpStatusCode.Unauthorized, "{\"object\":\"error\"}");

        var act = () => _mapper.ValidateAgainstDatabaseAsync(_client, DatabaseId, Options(("Title", "Name")));

        var exception = await act.Should().ThrowAsync<NotionApiException>();
        exception.Which.HttpStatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_NullClient_Throws()
    {
        var act = () => _mapper.ValidateAgainstDatabaseAsync(null!, DatabaseId, Options(("Title", "Name")));

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateAgainstDatabaseAsync_BlankDatabaseId_Throws()
    {
        var act = () => _mapper.ValidateAgainstDatabaseAsync(_client, " ", Options(("Title", "Name")));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Fake transport: returns one canned response and records the last request.
    /// </summary>
    private sealed class FakeDatabaseHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "{}";

        /// <summary>
        /// The most recent request sent through the handler.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>
        /// Sets the response returned for every subsequent request.
        /// </summary>
        public void Respond(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
