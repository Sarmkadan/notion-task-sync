# HttpClientFactoryExtensions

Extension methods for `IHttpClientFactory` that provide pre-configured `HttpClient` instances for interacting with JSON-based APIs, including specialized clients for authenticated requests, custom headers, and Notion-specific endpoints. These methods simplify client configuration by encapsulating common settings such as default request headers, JSON serialization behavior, and API versioning.

## API

### `public static HttpClient CreateJsonApiClient(IHttpClientFactory factory)`

Creates a pre-configured `HttpClient` instance with default JSON API settings, including `Accept: application/json` and `Content-Type: application/json` headers. The client is suitable for general JSON-based API interactions.

- **Parameters**:
  - `factory`: The `IHttpClientFactory` instance used to create the client.
- **Return Value**: A configured `HttpClient` instance.
- **Exceptions**: Throws `ArgumentNullException` if `factory` is `null`.

---

### `public static HttpClient CreateAuthenticatedJsonApiClient(IHttpClientFactory factory, string token)`

Creates a pre-configured `HttpClient` instance with JSON API settings and an `Authorization` header using the provided bearer token. Intended for authenticated API requests.

- **Parameters**:
  - `factory`: The `IHttpClientFactory` instance used to create the client.
  - `token`: The bearer token used for authentication.
- **Return Value**: A configured `HttpClient` instance with an `Authorization` header.
- **Exceptions**:
  - Throws `ArgumentNullException` if `factory` is `null`.
  - Throws `ArgumentNullException` if `token` is `null` or empty.

---

### `public static HttpClient CreateCustomHeadersClient(IHttpClientFactory factory, IEnumerable<KeyValuePair<string, string>> headers)`

Creates a pre-configured `HttpClient` instance with custom headers applied. The client retains default JSON API headers unless overridden by the provided headers.

- **Parameters**:
  - `factory`: The `IHttpClientFactory` instance used to create the client.
  - `headers`: A collection of header key-value pairs to apply to the client.
- **Return Value**: A configured `HttpClient` instance with the specified headers.
- **Exceptions**:
  - Throws `ArgumentNullException` if `factory` is `null`.
  - Throws `ArgumentNullException` if `headers` is `null`.

---
### `public static HttpClient CreateNotionClientWithVersion(IHttpClientFactory factory, string version)`

Creates a pre-configured `HttpClient` instance for interacting with the Notion API, including the `Notion-Version` header set to the specified version. Suitable for Notion-specific operations.

- **Parameters**:
  - `factory`: The `IHttpClientFactory` instance used to create the client.
  - `version`: The Notion API version to use (e.g., `"2022-06-28"`).
- **Return Value**: A configured `HttpClient` instance with the `Notion-Version` header.
- **Exceptions**:
  - Throws `ArgumentNullException` if `factory` is `null`.
  - Throws `ArgumentNullException` if `version` is `null` or empty.

---
### `public static async Task<string> GetStringAsync(HttpClient client, string requestUri)`

Sends a GET request to the specified URI and asynchronously returns the response body as a string. The client is expected to be pre-configured with appropriate headers (e.g., JSON or Notion-specific).

- **Parameters**:
  - `client`: The `HttpClient` instance to use for the request.
  - `requestUri`: The URI of the resource to request.
- **Return Value**: The response body as a string.
- **Exceptions**:
  - Throws `ArgumentNullException` if `client` or `requestUri` is `null`.
  - Throws `HttpRequestException` if the request fails (e.g., network issues, non-success status code).
  - Throws `TaskCanceledException` if the request times out.

---
### `public static async Task<string> PostJsonAsync(HttpClient client, string requestUri, object payload)`

Sends a POST request to the specified URI with a JSON payload and asynchronously returns the response body as a string. The client is expected to be pre-configured with JSON headers.

- **Parameters**:
  - `client`: The `HttpClient` instance to use for the request.
  - `requestUri`: The URI of the resource to request.
  - `payload`: The object to serialize as JSON and send in the request body.
- **Return Value**: The response body as a string.
- **Exceptions**:
  - Throws `ArgumentNullException` if `client`, `requestUri`, or `payload` is `null`.
  - Throws `HttpRequestException` if the request fails (e.g., network issues, non-success status code).
  - Throws `TaskCanceledException` if the request times out.
  - Throws `JsonException` if the payload cannot be serialized to JSON.

## Usage

### Example 1: Fetching data from a JSON API
