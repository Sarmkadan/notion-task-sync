// ... existing content ...

## NotionApiServiceTestsExtensions 

The `NotionApiServiceTestsExtensions` class provides a set of utility methods for creating test double responses from the Notion API. These extensions facilitate unit testing of components that interact with the Notion API by allowing you to simulate various API responses.

### Usage Example

Here's an example of how to use `NotionApiServiceTestsExtensions` to create a mock JSON response:

```csharp
var mockResponse = NotionApiServiceTestsExtensions.CreateMockJsonResponse<MyResponseType>("some-data");

var httpClient = new HttpClient(new MockHttpMessageHandler(mockResponse));

var notionApiService = new NotionApiService(httpClient);

var response = await notionApiService.GetAsync("https://api.notion.com/v1/some-endpoint");

// Assert response content
Assert.Equal("some-data", await response.Content.ReadAsStringAsync());
```

Similarly, you can use `SetupSuccessResponse` to set up a successful response or `SetupThrows` to simulate an exception:

```csharp
NotionApiServiceTestsExtensions.SetupSuccessResponse(httpClient, "some-endpoint", "success-data");

// OR

NotionApiServiceTestsExtensions.SetupThrows(httpClient, "some-endpoint", new Exception("Test exception"));
```

These extensions can significantly simplify testing of Notion API interactions by providing a controlled environment for simulating API responses.

## HttpClientFactoryExtensions

`HttpClientFactoryExtensions` offers a collection of helper methods for creating `HttpClient` instances that are pre‑configured for common scenarios such as JSON APIs, authenticated requests, custom headers, and Notion‑specific versioning. It also includes async convenience methods for sending GET and POST requests that automatically handle JSON payloads and response strings.

### Usage Example

```csharp
using Integration;

// Create a client that automatically sends JSON and includes an API key header
var client = HttpClientFactoryExtensions.CreateAuthenticatedJsonApiClient(
    baseAddress: new Uri("https://api.example.com/"),
    apiKey: "my-secret-key");

// Perform a simple GET request and read the response as a string
string getResult = await HttpClientFactoryExtensions.GetStringAsync(
    client,
    "/v1/items");

// Post a JSON payload and obtain the response string
var payload = new { Name = "New Item", Quantity = 5 };
string postResult = await HttpClientFactoryExtensions.PostJsonAsync(
    client,
    "/v1/items",
    payload);

// If you need a client with custom headers only
var customClient = HttpClientFactoryExtensions.CreateCustomHeadersClient(
    baseAddress: new Uri("https://api.custom.com/"),
    headers: new Dictionary<string, string>
    {
        { "X-Custom-Header", "value" },
        { "User-Agent", "TaskFactory/1.0" }
    });

// For Notion API calls that require a specific Notion version header
var notionClient = HttpClientFactoryExtensions.CreateNotionClientWithVersion(
    baseAddress: new Uri("https://api.notion.com/v1/"),
    notionVersion: "2022-06-28");

// Or a plain JSON API client without authentication
var jsonClient = HttpClientFactoryExtensions.CreateJsonApiClient(
    baseAddress: new Uri("https://public-api.example.com/"));
```

These extensions reduce boilerplate when configuring `HttpClient` instances and make async HTTP interactions concise and type‑safe.

// ... existing content ...
