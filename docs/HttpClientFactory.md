# HttpClientFactory

Central factory for creating and managing pre-configured `HttpClient` instances tailored for Notion API communication. It encapsulates authentication, rate-limit awareness, and header configuration, ensuring consistent client setup across the application while properly handling disposal of owned resources.

## API

### `public HttpClientFactory`

Constructs a new instance of the factory. Initializes internal configuration and resource tracking necessary for subsequent client creation methods.

**Parameters:** None

**Returns:** A new `HttpClientFactory` instance.

**Throws:** May throw if internal initialization of configuration or underlying handler resources fails.

---

### `public HttpClient GetNotionHttpClient`

Retrieves a fully configured `HttpClient` specifically optimized for Notion API endpoints. The returned client includes authentication headers, rate-limit handling, and any Notion-specific base address or default headers required by the service.

**Parameters:** None

**Returns:** `HttpClient` — A pre-configured client ready for Notion API requests.

**Throws:** May throw if the underlying handler or client creation fails, or if required configuration for Notion connectivity is missing.

---

### `public HttpClient CreateGenericHttpClient`

Creates a basic `HttpClient` without Notion-specific authentication or rate-limiting. Suitable for general-purpose HTTP requests where Notion API credentials are not required.

**Parameters:** None

**Returns:** `HttpClient` — A generic client with default settings.

**Throws:** May throw if the underlying handler or client creation fails.

---

### `public HttpClient CreateAuthenticatedHttpClient`

Creates an `HttpClient` with authentication headers applied but without rate-limit awareness. Useful for endpoints that require authorization but do not impose strict rate limits or where custom rate handling is desired.

**Parameters:** None

**Returns:** `HttpClient` — An authenticated client without built-in rate limiting.

**Throws:** May throw if authentication configuration is missing or invalid, or if client creation fails.

---

### `public HttpClient CreateRateLimitAwareHttpClient`

Creates an `HttpClient` with rate-limit handling middleware applied but without authentication headers. Intended for scenarios where rate limiting is needed independently of Notion authentication.

**Parameters:** None

**Returns:** `HttpClient` — A rate-limit-aware client without authentication.

**Throws:** May throw if rate-limiting middleware configuration fails or client creation fails.

---

### `public void Dispose`

Releases all resources held by the factory, including any internally managed `HttpClient` instances and their underlying handlers. After disposal, further calls to creation methods may fail.

**Parameters:** None

**Returns:** void

**Throws:** Does not throw; safe to call multiple times.

---

### `public void ConfigureHeaders`

Applies or updates header configuration on the factory. Subsequent client creation methods will include these headers in the produced `HttpClient` instances.

**Parameters:** (Specific parameters depend on implementation — typically accepts a collection of header names and values or an action delegate for header customization.)

**Returns:** void

**Throws:** May throw if header values are malformed or violate HTTP header specifications.

## Usage

### Example 1: Standard Notion API Request

```csharp
using var factory = new HttpClientFactory();
factory.ConfigureHeaders(/* custom headers if needed */);

var notionClient = factory.GetNotionHttpClient();
var response = await notionClient.GetAsync("https://api.notion.com/v1/pages/my-page-id");

if (response.IsSuccessStatusCode)
{
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine(content);
}
```

### Example 2: Mixed Client Scenarios

```csharp
using var factory = new HttpClientFactory();

// Authenticated request to a third-party service that integrates with Notion
var authClient = factory.CreateAuthenticatedHttpClient();
await authClient.PostAsync("https://partner-api.example.com/sync", new StringContent("{...}"));

// Rate-limited polling of a public endpoint
var rateLimitedClient = factory.CreateRateLimitAwareHttpClient();
for (int i = 0; i < 10; i++)
{
    var result = await rateLimitedClient.GetAsync("https://public-data.example.com/feed");
    // Rate-limit middleware handles delays automatically
    Console.WriteLine($"Poll {i}: {result.StatusCode}");
}
```

## Notes

- **Thread Safety:** Instance methods on `HttpClientFactory` are not guaranteed to be thread-safe. Clients should create and use the factory from a single thread or synchronize access externally. The `HttpClient` instances returned are themselves thread-safe for concurrent requests.
- **Disposal:** Calling `Dispose` on the factory disposes all internally tracked handlers and clients. Do not use `HttpClient` instances obtained from a disposed factory, as their underlying handlers will be invalidated.
- **Header Configuration:** `ConfigureHeaders` should be called before creating clients to ensure headers are propagated. Headers applied after client creation do not retroactively affect already-returned instances.
- **Rate Limiting:** `CreateRateLimitAwareHttpClient` applies middleware that may introduce delays or retries. Ensure calling code accounts for potentially increased latency.
- **Authentication Scope:** `GetNotionHttpClient` combines both authentication and rate limiting. Use the specialized creation methods when only one aspect is needed to avoid unnecessary overhead or header leakage to non-Notion endpoints.
