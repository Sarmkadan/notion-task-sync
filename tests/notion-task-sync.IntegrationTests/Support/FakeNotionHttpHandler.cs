#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.IntegrationTests.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Scripted fake for the Notion REST API that can answer with any status code and headers.
/// Each queued response is returned for exactly one request, in order. Every request is captured
/// with its full header set so tests can assert on authentication and version headers.
/// No socket is opened; the handler only ever sees requests made through the <see cref="HttpClient"/> it backs.
/// </summary>
public sealed class FakeNotionHttpHandler : HttpMessageHandler
{
    private readonly Queue<ScriptedResponse> _responses = new();
    private readonly List<CapturedRequest> _requests = new();

    /// <summary>Requests received by this handler, in order.</summary>
    public IReadOnlyList<CapturedRequest> Requests => _requests;

    /// <summary>
    /// Queues a JSON response for the next request.
    /// </summary>
    /// <param name="statusCode">HTTP status to return.</param>
    /// <param name="json">Response body.</param>
    /// <param name="retryAfterSeconds">When set, adds a <c>Retry-After</c> header with this delay in seconds.</param>
    public void EnqueueJson(HttpStatusCode statusCode, string json, int? retryAfterSeconds = null)
    {
        _responses.Enqueue(new ScriptedResponse(statusCode, json, retryAfterSeconds));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var uri = request.RequestUri ?? throw new InvalidOperationException("Request has no URI.");
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .GroupBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.SelectMany(h => h.Value).ToArray(), StringComparer.OrdinalIgnoreCase);

        _requests.Add(new CapturedRequest(request.Method, uri, headers, body));

        if (_responses.Count == 0)
            throw new InvalidOperationException($"No scripted response left for {request.Method} {uri}.");

        var scripted = _responses.Dequeue();
        var response = new HttpResponseMessage(scripted.StatusCode)
        {
            Content = new StringContent(scripted.Json, Encoding.UTF8, "application/json"),
        };

        if (scripted.RetryAfterSeconds is int seconds)
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));

        return response;
    }

    private sealed record ScriptedResponse(HttpStatusCode StatusCode, string Json, int? RetryAfterSeconds);
}

/// <summary>
/// A single request captured by <see cref="FakeNotionHttpHandler"/>.
/// </summary>
/// <param name="Method">HTTP method.</param>
/// <param name="Uri">Full request URI.</param>
/// <param name="Headers">Request and content headers, keyed case-insensitively.</param>
/// <param name="Body">Request body as sent (empty for bodiless requests).</param>
public sealed record CapturedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string[]> Headers,
    string Body);
