#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.IntegrationTests.Support;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Stands in for the Notion REST API inside an <see cref="HttpClient"/>.
/// Responses are replayed in order from JSON fixtures in the <c>Fixtures</c> folder, and every
/// request is recorded so tests can assert on the URL and body that were sent.
/// Requests to any host other than <c>api.notion.com</c> are rejected, and no socket is ever opened.
/// </summary>
public sealed class RecordedNotionHttpHandler : HttpMessageHandler
{
    private const string NotionHost = "api.notion.com";

    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private readonly Queue<string> _responseBodies = new();
    private readonly List<RecordedRequest> _requests = new();

    /// <summary>Requests sent through this handler, in order.</summary>
    public IReadOnlyList<RecordedRequest> Requests => _requests;

    /// <summary>
    /// Queues the body of a recorded Notion response for the next request.
    /// </summary>
    /// <param name="fileName">Fixture file name inside the <c>Fixtures</c> folder.</param>
    public void EnqueueFixture(string fileName)
    {
        var path = Path.Combine(FixtureDirectory, fileName);
        _responseBodies.Enqueue(File.ReadAllText(path));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var uri = request.RequestUri ?? throw new InvalidOperationException("Request has no URI.");
        _requests.Add(new RecordedRequest(request.Method, uri, body));

        if (!string.Equals(uri.Host, NotionHost, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unexpected host in integration test: {uri.Host}");

        if (_responseBodies.Count == 0)
            throw new InvalidOperationException($"No recorded response left for {request.Method} {uri}.");

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseBodies.Dequeue(), Encoding.UTF8, "application/json"),
        };
    }
}

/// <summary>
/// A single request captured by <see cref="RecordedNotionHttpHandler"/>.
/// </summary>
/// <param name="Method">HTTP method.</param>
/// <param name="Uri">Full request URI.</param>
/// <param name="Body">Request body as sent (empty for bodiless requests).</param>
public sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Body);
