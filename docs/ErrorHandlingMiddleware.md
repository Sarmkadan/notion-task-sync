# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides a centralized mechanism for catching, classifying, and formatting errors that occur during the execution of asynchronous or synchronous operations. It is designed to be used in the `notion-task-sync` pipeline to standardize error handling, produce consistent error messages, and determine whether an operation can be retried. The middleware exposes both a generic `TryExecute` pattern for wrapping arbitrary work and a top-level `ExecuteAsync` entry point for the middleware itself.

## API

### `public ErrorHandlingMiddleware`

The constructor. Initializes a new instance of the middleware. No parameters are shown in the public signature; the constructor may accept configuration (e.g., logging dependencies) that is not part of the public API.

### `public async Task<(T? result, bool success, string? error)> TryExecuteAsync<T>(...)`

Executes an asynchronous operation and returns a tuple indicating success or failure.

- **Parameters**: The method accepts a delegate or function that returns a `Task<T>`. The exact parameter list is not exposed in the public signature; typical usage passes a lambda or method group.
- **Returns**: A tuple `(T? result, bool success, string? error)`. If the operation succeeds, `success` is `true`, `result` contains the value, and `error` is `null`. If an exception is caught, `success` is `false`, `result` is `default`, and `error` contains a formatted error message.
- **Throws**: Does not throw exceptions to the caller; all exceptions are caught and converted into the error string. However, fatal exceptions (e.g., `StackOverflowException`, `OutOfMemoryException`) may still propagate.

### `public (T? result, bool success, string? error) TryExecute<T>(...)`

Synchronous counterpart of `TryExecuteAsync`. Executes a synchronous operation and returns the same tuple structure.

- **Parameters**: A delegate or function that returns `T`.
- **Returns**: Same tuple semantics as `TryExecuteAsync`.
- **Throws**: Same exception handling policy as the async version.

### `public int GetStatusCode`

Returns the HTTP status code associated with the most recently handled error.

- **Parameters**: None.
- **Returns**: An integer representing the HTTP status code (e.g., 400, 500). The value is determined by the middleware based on the exception type or custom mapping.
- **Throws**: No exceptions.

### `public string FormatErrorMessage`

Returns a formatted error message string. The exact formatting logic is internal; the method may incorporate the status code, exception details, and a human-readable description.

- **Parameters**: None.
- **Returns**: A string containing the formatted error message. Returns an empty string if no error has been recorded.
- **Throws**: No exceptions.

### `public bool IsRetryable`

Indicates whether the most recently handled error is considered retryable.

- **Parameters**: None.
- **Returns**: `true` if the error is transient and the operation can be retried; otherwise `false`.
- **Throws**: No exceptions.

### `public async Task ExecuteAsync(...)`

The main entry point for the middleware. Invokes the next component in the pipeline and applies error handling logic.

- **Parameters**: The method likely accepts an `HttpContext` or similar context object, but the exact signature is not part of the public API shown.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: Does not throw; all exceptions are caught and handled internally, producing appropriate responses (e.g., setting the HTTP status code and writing the error message).

## Usage

### Example 1: Wrapping an asynchronous operation with `TryExecuteAsync`

```csharp
var middleware = new ErrorHandlingMiddleware();

// Simulate an async operation that may fail
async Task<string> FetchDataAsync()
{
    // ... API call that could throw
    return await httpClient.GetStringAsync("https://api.example.com/data");
}

var (result, success, error) = await middleware.TryExecuteAsync(FetchDataAsync);

if (success)
{
    Console.WriteLine($"Data: {result}");
}
else
{
    Console.WriteLine($"Error: {error}");
    if (middleware.IsRetryable)
    {
        // Schedule a retry
    }
}
```

### Example 2: Using the middleware in an ASP.NET Core pipeline

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.Run(async context =>
    {
        // This code runs inside the middleware's error handling scope
        await context.Response.WriteAsync("Hello, world!");
    });
}
```

In this scenario, the middleware automatically catches any unhandled exceptions from downstream middleware, sets the appropriate status code via `GetStatusCode`, and writes the formatted error message to the response.

## Notes

- **Thread safety**: Instances of `ErrorHandlingMiddleware` are not guaranteed to be thread-safe. Properties such as `GetStatusCode`, `FormatErrorMessage`, and `IsRetryable` reflect the state of the most recently handled error. If multiple threads execute operations concurrently on the same instance, the state may be overwritten. For concurrent scenarios, create a new instance per operation or use synchronization.
- **Null handling**: The `TryExecute` methods return `default(T)` for `result` when an error occurs. If `T` is a reference type, `result` will be `null`. Callers should always check the `success` flag before using `result`.
- **Error classification**: The middleware uses internal heuristics to determine status codes and retryability. Exceptions such as `HttpRequestException` may be mapped to 503 (Service Unavailable) and marked retryable, while `ArgumentException` may map to 400 (Bad Request) and be non-retryable.
- **Empty state**: Before any operation has been executed, `GetStatusCode` returns 0, `FormatErrorMessage` returns an empty string, and `IsRetryable` returns `false`.
