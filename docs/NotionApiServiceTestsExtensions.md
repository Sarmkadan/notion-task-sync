# NotionApiServiceTestsExtensions

A set of extension methods for unit testing classes that interact with the Notion API. These helpers simplify the setup of mock HTTP responses, validation of requests, and assertion of expected behavior when testing services that depend on `NotionApiService`.

## API

### `public static HttpResponseMessage CreateMockResponse()`
Creates a basic `HttpResponseMessage` with status code 200 (OK) and an empty content string. Useful when the response body is irrelevant to the test scenario.

### `public static HttpResponseMessage CreateMockJsonResponse<T>(T content)`
Creates an `HttpResponseMessage` with status code 200 (OK) and a JSON-serialized body of the provided object. The response includes the `Content-Type: application/json` header.

- **Parameters**:
  - `content`: The object to serialize as JSON in the response body.
- **Return value**: An `HttpResponseMessage` with the serialized JSON content.

### `public static void SetupSuccessResponse(this Mock<HttpMessageHandler> handler)`
Configures the mock `HttpMessageHandler` to return a success response (status 200) for any request. Overrides any previous setup for the handler.

- **Parameters**:
  - `handler`: The mock `HttpMessageHandler` to configure.

### `public static void SetupThrows(this Mock<HttpMessageHandler> handler, HttpRequestException exception)`
Configures the mock `HttpMessageHandler` to throw the specified `HttpRequestException` for any request. Useful for simulating network or API errors.

- **Parameters**:
  - `handler`: The mock `HttpMessageHandler` to configure.
  - `exception`: The exception to throw when a request is made.

### `public static void VerifySendAsync(this Mock<HttpMessageHandler> handler, string expectedMethod, string expectedUri, Times? times = null)`
Verifies that the mock `HttpMessageHandler` received exactly one request matching the specified method and URI. Supports optional times verification.

- **Parameters**:
  - `handler`: The mock `HttpMessageHandler` to verify.
  - `expectedMethod`: The expected HTTP method (e.g., `"GET"`).
  - `expectedUri`: The expected request URI (e.g., `"/v1/pages"`).
  - `times`: Optional `Times` instance to specify expected invocation count (default: `Times.Once()`).

### `public static DomainTask CreateTestTask()`
Creates a `DomainTask` instance with default values for testing. Useful when the task properties are not relevant to the test scenario.

- **Return value**: A `DomainTask` with default or minimal valid values.

### `public static DomainTask CreateTestTask(this NotionApiServiceTestsExtensions)`
Overload of `CreateTestTask` that allows inline usage within test classes. Returns a `DomainTask` with default values.

- **Return value**: A `DomainTask` with default or minimal valid values.

### `public static async System.Threading.Tasks.Task AssertValidationExceptionAsync(this Task<DomainTask> task, string expectedMessage)`
Asserts that the provided task throws a `ValidationException` with the specified message. Useful for testing input validation logic.

- **Parameters**:
  - `task`: The task to assert against.
  - `expectedMessage`: The expected error message in the exception.
- **Return value**: A `Task` representing the assertion.
- **Throws**: `XunitException` if the task does not throw a `ValidationException` or if the message does not match.

### `public static HttpResponseMessage CreatePaginationResponse<T>(IEnumerable<T> items, string nextCursor = null)`
Creates an `HttpResponseMessage` with a paginated JSON response containing the provided items and an optional next cursor. The response includes the `Content-Type: application/json` header.

- **Parameters**:
  - `items`: The collection of items to include in the response.
  - `nextCursor`: Optional cursor for pagination (default: `null`).
- **Return value**: An `HttpResponseMessage` with the paginated JSON content.

### `public static void SetupMultiPageResponse(this Mock<HttpMessageHandler> handler, IEnumerable<HttpResponseMessage> responses)`
Configures the mock `HttpMessageHandler` to return a sequence of responses for subsequent requests. Useful for testing pagination or retry logic.

- **Parameters**:
  - `handler`: The mock `HttpMessageHandler` to configure.
  - `responses`: The sequence of responses to return in order.

## Usage

### Example 1: Testing a successful API call
