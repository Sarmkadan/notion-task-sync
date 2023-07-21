# DependencyInjection

Utility class for configuring dependency injection and validating application configuration in the Notion Task Sync project.

## API

### `AddApplicationServices`

Registers core application services with the dependency injection container. This includes domain services, application services, and infrastructure components required for task synchronization.

- **Parameters**
  - `services`: The `IServiceCollection` to which services are added.
- **Return Value**
  - Returns the `IServiceCollection` for method chaining.
- **Exceptions**
  - Throws `ConfigurationException` if required configuration is missing or invalid.

### `ValidateConfiguration`

Validates the application configuration, ensuring required settings are present and valid for task synchronization.

- **Parameters**
  - `configuration`: The `IConfiguration` instance to validate.
- **Exceptions**
  - Throws `ConfigurationException` if validation fails.

### `AddHttpClients`

Registers HTTP client configurations for external service communication, including Notion API clients and optional third-party integrations.

- **Parameters**
  - `services`: The `IServiceCollection` to which HTTP clients are added.
  - `configuration`: The `IConfiguration` used to configure HTTP clients.
- **Return Value**
  - Returns the `IServiceCollection` for method chaining.
- **Exceptions**
  - Throws `ConfigurationException` if required HTTP client configurations are missing.

### `ConfigurationException(string message)`

Initializes a new instance of the `ConfigurationException` class with a specified error message.

- **Parameters**
  - `message`: The message describing the exception.

### `ConfigurationException(string message, Exception inner)`

Initializes a new instance of the `ConfigurationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

- **Parameters**
  - `message`: The message describing the exception.
  - `inner`: The exception that is the cause of the current exception.

## Usage

### Basic Service Registration
