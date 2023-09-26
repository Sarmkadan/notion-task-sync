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

// ... existing content ...
