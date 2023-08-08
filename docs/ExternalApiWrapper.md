# ExternalApiWrapper

`ExternalApiWrapper` is a generic HTTP client abstraction for interacting with external APIs in the `notion-task-sync` project. It encapsulates the boilerplate of serialization, request construction, and response handling, exposing typed asynchronous methods for the standard HTTP verbs GET, POST, PUT, and DELETE. The class is designed to reduce repetitive `HttpClient` code and centralize common concerns such as JSON media type negotiation and status-code-based error translation.

## API

### `public ExternalApiWrapper`
The constructor. Initializes a new instance of the wrapper, presumably accepting an `HttpClient` or configuration that defines the base address, default headers, and serialization options used by all subsequent calls. Details of the constructor parameters are internal; the public surface guarantees that a properly configured instance is ready to issue requests immediately after construction.

### `public async Task<T?> GetAsync<T>(...)`
Performs an HTTP GET request to a specified resource and deserializes a successful response body into an instance of `T`.

- **Purpose**: Retrieve a single resource representation.
- **Parameters**: Accepts at least a request URI or a combination of relative path and route parameters. The exact overloads are internal, but the intent is to identify the target resource.
- **Returns**: A `Task<T?>` that resolves to the deserialized object of type `T` when the server returns a success status code (2xx) with a JSON body. Returns `null` when the server responds with a success status code but an empty body, or when the response indicates a non-success status that is handled without an exception (implementation-defined).
- **Throws**: Throws an exception derived from `HttpRequestException` or a custom domain exception when the server returns a non-success status code that the wrapper treats as a failure, when the response body cannot be deserialized into `T`, or when a network-level failure occurs (timeout, DNS resolution failure, etc.).

### `public async Task<T?> PostAsync<T>(...)`
Performs an HTTP POST request, sending a serialized payload and deserializing the response body into an instance of `T`.

- **Purpose**: Create a new resource or submit data to an endpoint that returns a structured result.
- **Parameters**: Accepts a target URI and a request body object that will be serialized as JSON. Additional routing or header parameters may be present in internal overloads.
- **Returns**: A `Task<T?>` that resolves to the deserialized response body of type `T` on success. Returns `null` under the same conditions as `GetAsync<T>`.
- **Throws**: Throws under the same conditions as `GetAsync<T>`, with the additional possibility of a serialization exception if the provided request body cannot be serialized to JSON.

### `public async Task<T?> PutAsync<T>(...)`
Performs an HTTP PUT request, sending a serialized payload that represents a full replacement of the target resource, and deserializes the response body into an instance of `T`.

- **Purpose**: Fully update or replace an existing resource.
- **Parameters**: Accepts a target URI and a request body object to be serialized as JSON.
- **Returns**: A `Task<T?>` that resolves to the deserialized response body of type `T` on success. Returns `null` under the same conditions as `GetAsync<T>`.
- **Throws**: Throws under the same conditions as `PostAsync<T>`.

### `public async Task<bool> DeleteAsync(...)`
Performs an HTTP DELETE request and returns a boolean indicating success.

- **Purpose**: Remove a resource.
- **Parameters**: Accepts at least a target URI identifying the resource to delete.
- **Returns**: A `Task<bool>` that resolves to `true` when the server responds with a success status code (typically 2xx, most commonly 200 or 204). Resolves to `false` when the server responds with a non-success status code that the wrapper handles without throwing (e.g., a 404 Not Found may be treated as a non-exceptional negative result).
- **Throws**: Throws an exception on network failures or when the server returns a non-success status code that the wrapper’s policy considers exceptional (e.g., 5xx server errors).

## Usage

### Example 1: Fetching a Notion page
```csharp
// Assume wrapper is injected via constructor
public async Task<NotionPage?> GetPageAsync(string pageId)
{
    var page = await _api.GetAsync<NotionPage>($"pages/{pageId}");
    return page;
}
```
This example demonstrates a straightforward GET request to retrieve a typed representation of a Notion page. The caller receives either a populated `NotionPage` object or `null`, and can handle a thrown exception for network or authorization failures at a higher level.

### Example 2: Updating a task and handling deletion
```csharp
public async Task<bool> SyncTaskAsync(TaskItem task)
{
    // Update the remote task
    var updated = await _api.PutAsync<TaskItem>($"tasks/{task.RemoteId}", task);
    if (updated is null)
    {
        // Update returned no content — treat as success but log
        _logger.LogWarning("PUT returned null for task {Id}", task.RemoteId);
    }

    // If the task is marked for deletion locally, delete remotely
    if (task.IsDeleted)
    {
        bool deleted = await _api.DeleteAsync($"tasks/{task.RemoteId}");
        return deleted;
    }

    return true;
}
```
This example chains a PUT and a conditional DELETE. The boolean return from `DeleteAsync` allows the caller to branch on whether the remote resource was actually removed, while the nullable return from `PutAsync` accommodates endpoints that may return 204 No Content.

## Notes

- **Null returns**: The nullable return types `T?` on GET, POST, and PUT reflect the reality that some successful HTTP responses carry no body (e.g., 204 No Content) or that the wrapper may internally handle certain status codes by returning `null` rather than throwing. Callers should always null-check the result before dereferencing.
- **Exception granularity**: The wrapper throws on network failures, deserialization failures, and server errors deemed fatal. The exact set of status codes that trigger exceptions versus null/false returns is determined by the wrapper’s internal policy. Callers should not rely on exceptions for control flow in cases like 404 on DELETE, which may simply return `false`.
- **Thread safety**: The public methods are asynchronous and return tasks. The underlying `HttpClient` instance is expected to be managed according to best practices (typically registered as a singleton via dependency injection). The methods themselves do not mutate shared state and are safe to call concurrently from multiple threads, assuming the underlying `HttpClient` and any serialization configuration are themselves thread-safe.
- **Serialization coupling**: All methods that accept or return typed bodies assume JSON as the wire format. Supplying a request body that cannot be serialized by the configured serializer will result in an exception before the request is sent.
- **Disposal**: `ExternalApiWrapper` does not expose a public `Dispose` method. Lifetime management of the underlying `HttpClient` is the responsibility of the code that constructs the wrapper, typically the dependency injection container.
