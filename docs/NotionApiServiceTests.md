# NotionApiServiceTests

Unit test class for `NotionApiService`, verifying correct interaction with the Notion API including authentication, request construction, pagination, error handling, and data transformation.

## API

### Public Members

#### `NotionApiServiceTests`
Constructor for the test class. Initializes test dependencies and mocks.

#### `FetchPagesAsync_WithValidDatabaseId_ReturnsPages`
Verifies that calling `FetchPagesAsync` with a valid database ID returns a non-empty collection of pages. No exceptions are expected.

#### `FetchPagesAsync_WithEmptyDatabaseId_ThrowsValidationException`
Verifies that calling `FetchPagesAsync` with an empty database ID throws a `ValidationException`.

#### `FetchPagesAsync_WithNullDatabaseId_ThrowsValidationException`
Verifies that calling `FetchPagesAsync` with a null database ID throws a `ValidationException`.

#### `FetchPagesAsync_WithDefaultPageSize_UsesPaginationCorrectly`
Verifies that calling `FetchPagesAsync` with the default page size (100) results in correct pagination behavior, including multiple API calls if necessary.

#### `FetchPagesAsync_WhenHttpRequestFails_ThrowsNotionApiException`
Verifies that when the underlying HTTP request fails, a `NotionApiException` is thrown.

#### `FetchPagesSinceAsync_WithValidInputs_ReturnsFilteredPages`
Verifies that calling `FetchPagesSinceAsync` with valid database ID and timestamp returns a filtered collection of pages edited after the given timestamp.

#### `FetchPagesSinceAsync_WithEmptyDatabaseId_ThrowsValidationException`
Verifies that calling `FetchPagesSinceAsync` with an empty database ID throws a `ValidationException`.

#### `FetchPagesSinceAsync_FiltersByLastEditedTime`
Verifies that `FetchPagesSinceAsync` correctly filters results by the `last_edited_time` field.

#### `FetchPagesSinceAsync_WithFutureTimestamp_ReturnsEmptyList`
Verifies that calling `FetchPagesSinceAsync` with a future timestamp returns an empty list.

#### `Constructor_WithValidApiKey_SetsAuthHeader`
Verifies that constructing the service with a valid API key sets the Authorization header with a Bearer token.

#### `Constructor_WithNullApiKey_CreatesServiceWithoutAuth`
Verifies that constructing the service with a null API key creates a service instance without an Authorization header.

#### `Constructor_WithoutHttpClient_CreatesDefaultHttpClient`
Verifies that constructing the service without an `HttpClient` results in a default `HttpClient` being created.

#### `FetchPagesAsync_HandlesEmptyResults`
Verifies that `FetchPagesAsync` correctly handles and returns an empty result set without throwing.

#### `CreatePageAsync_WithValidTask_CreatesPage`
Verifies that calling `CreatePageAsync` with a valid task creates a page in the database.

#### `UpdatePageAsync_WithValidPageAndTask_UpdatesPage`
Verifies that calling `UpdatePageAsync` with a valid page ID and task updates the corresponding page.

#### `CreatePageAsync_WithEmptyDatabaseId_ThrowsValidationException`
Verifies that calling `CreatePageAsync` with an empty database ID throws a `ValidationException`.

#### `UpdatePageAsync_WithEmptyPageId_ThrowsValidationException`
Verifies that calling `UpdatePageAsync` with an empty page ID throws a `ValidationException`.

#### `FetchPagesAsync_IncludesNotionApiVersionHeader`
Verifies that `FetchPagesAsync` includes the Notion API version header (`Notion-Version`) in the request.

#### `FetchPagesAsync_IncludesBearerTokenInAuthHeader`
Verifies that `FetchPagesAsync` includes a Bearer token in the Authorization header when an API key is provided.

## Usage

### Example 1: Testing Page Fetching
