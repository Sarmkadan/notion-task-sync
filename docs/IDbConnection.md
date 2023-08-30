# IDbConnection

The `IDbConnection` class represents a database connection configuration within the Notion Task Sync project. It stores the connection string and database name required to establish a connection, and also provides constructors for creating a `DbConnectionException` that can be thrown when connection operations fail. This class is used to centralize connection metadata and error handling for database interactions.

## API

### `DbConnectionException(string message)`

Initializes a new instance of the `DbConnectionException` class with a specified error message.

- **Parameters**:  
  `message` – A string describing the error that occurred.
- **Return value**: None (constructor).
- **Throws**: None.

### `DbConnectionException(string message, Exception inner)`

Initializes a new instance of the `DbConnectionException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

- **Parameters**:  
  `message` – A string describing the error that occurred.  
  `inner` – The exception that is the cause of the current exception.
- **Return value**: None (constructor).
- **Throws**: None.

### `string? ConnectionString`

Gets or sets the connection string used to connect to the database.

- **Type**: `string?` – The connection string, or `null` if not set.
- **Throws**: None.

### `string? DatabaseName`

Gets or sets the name of the target database.

- **Type**: `string?` – The database name, or `null` if not set.
- **Throws**: None.

## Usage

### Example 1: Creating a connection configuration and handling a connection error

```csharp
var connection = new IDbConnection
{
    ConnectionString = "Server=localhost;Database=NotionSync;Trusted_Connection=True;",
    DatabaseName = "NotionSync"
};

try
{
    // Attempt to open a connection using the configuration
    OpenDatabaseConnection(connection);
}
catch (Exception ex)
{
    throw new IDbConnection.DbConnectionException("Failed to open database connection.", ex);
}
```

### Example 2: Throwing a connection exception with a simple message

```csharp
public void ValidateConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new IDbConnection.DbConnectionException("Connection string cannot be null or empty.");
    }
}
```

## Notes

- The `ConnectionString` and `DatabaseName` properties are nullable; consumers should check for `null` before using them in connection logic to avoid unexpected behavior.
- The `DbConnectionException` constructors are intended to be used when a database connection operation fails. They do not perform any validation on the provided arguments.
- Instances of `IDbConnection` are not thread-safe. If the same instance is accessed concurrently from multiple threads, external synchronization (e.g., a lock) must be used to avoid race conditions when reading or writing the properties.
- The class does not implement `IDisposable`; any underlying connection resources must be managed separately.
