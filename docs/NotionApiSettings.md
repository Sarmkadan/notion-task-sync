# NotionApiSettings
The `NotionApiSettings` type is used to configure and manage settings for interacting with the Notion API. It provides properties for specifying API keys, base URLs, request timeouts, and other settings that control how the API is accessed and utilized. This type is essential for customizing and fine-tuning the behavior of Notion API clients, allowing developers to adapt to different use cases and requirements.

## API
The `NotionApiSettings` type exposes the following public members:
* `ApiKey`: The API key used for authentication. It is a nullable string, allowing for cases where the API key might not be required or is not available.
* `BaseUrl`: The base URL of the Notion API. This is a required string property.
* `ApiVersion`: The version of the Notion API to use. This is a required string property.
* `RequestTimeoutSeconds`: The timeout in seconds for API requests. This is an integer property.
* `MaxRetries`: The maximum number of retries for failed API requests. This is an integer property.
* `RetryDelayMs`: The delay in milliseconds between retries. This is an integer property.
* `RateLimitPerMinute`: The rate limit per minute for API requests. This is an integer property.
* `RespectRateLimits`: A boolean indicating whether to respect rate limits. If `true`, the client will adhere to the specified rate limit.
* `DefaultPageSize`: The default page size for API requests. This is an integer property.
* `MaxPageSize`: The maximum page size for API requests. This is an integer property.
* `EnableCaching`: A boolean indicating whether caching is enabled. If `true`, the client will cache responses.
* `CacheDurationMinutes`: The duration in minutes that cached responses are valid. This is an integer property.
* `DatabaseIds`: A list of database IDs. This is a list of strings.
* `PropertyMappings`: A dictionary of property mappings. This is a dictionary of string keys and values.
* `Validate`: A boolean indicating whether to validate API responses. If `true`, the client will validate responses.
* `ToString()`: Returns a string representation of the `NotionApiSettings` instance.
* `GetMaskedApiKey()`: Returns the masked API key.

## Usage
Here are two examples of using the `NotionApiSettings` type:
```csharp
// Example 1: Creating a NotionApiSettings instance with default values
var settings = new NotionApiSettings
{
    ApiKey = "my_api_key",
    BaseUrl = "https://api.notion.com",
    ApiVersion = "2022-06-28",
    RequestTimeoutSeconds = 30,
    MaxRetries = 3,
    RetryDelayMs = 500,
    RateLimitPerMinute = 100,
    RespectRateLimits = true,
    DefaultPageSize = 100,
    MaxPageSize = 1000,
    EnableCaching = true,
    CacheDurationMinutes = 30,
    DatabaseIds = new List<string> { "my_database_id" },
    PropertyMappings = new Dictionary<string, string> { { "my_property", "my_value" } },
    Validate = true
};

// Example 2: Using the NotionApiSettings instance to configure an API client
var client = new NotionApiClient(settings);
var response = client.GetDatabase(settings.DatabaseIds[0]);
```

## Notes
When using the `NotionApiSettings` type, consider the following edge cases and thread-safety remarks:
* The `ApiKey` property is nullable, so it's essential to check for null before using it.
* The `BaseUrl` and `ApiVersion` properties are required, so ensure they are set before using the `NotionApiSettings` instance.
* The `RequestTimeoutSeconds`, `MaxRetries`, and `RetryDelayMs` properties control the request timeout and retry behavior. Adjust these values according to your specific use case.
* The `RateLimitPerMinute` and `RespectRateLimits` properties control the rate limiting behavior. Ensure you understand the implications of setting these values.
* The `DefaultPageSize` and `MaxPageSize` properties control the page size for API requests. Adjust these values according to your specific use case.
* The `EnableCaching` and `CacheDurationMinutes` properties control the caching behavior. Ensure you understand the implications of setting these values.
* The `DatabaseIds` and `PropertyMappings` properties are used to configure the API client. Ensure you understand the implications of setting these values.
* The `Validate` property controls the validation of API responses. Ensure you understand the implications of setting this value.
* The `ToString()` method returns a string representation of the `NotionApiSettings` instance. This can be useful for debugging purposes.
* The `GetMaskedApiKey()` method returns the masked API key. This can be useful for logging or auditing purposes.
* The `NotionApiSettings` type is not thread-safe by default. If you need to access the instance from multiple threads, consider implementing synchronization mechanisms to ensure thread safety.
