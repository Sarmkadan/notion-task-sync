# DatabaseContext

A lightweight wrapper around a database connection and transaction lifecycle, designed to simplify asynchronous operations against a Notion-sync target database. It provides connection management, transaction scoping, and query execution with built-in retry and cleanup semantics.

## API

### `public DatabaseContext`

Constructs a new `DatabaseContext` instance. The connection is not opened until `OpenAsync` is called. The context does not take ownership of the provided `SqlServerConnection`; disposal of the connection is the caller's responsibility unless managed externally.

### `public async Task OpenAsync()`

Opens the underlying database connection asynchronously. If the connection is already open, this method returns immediately without error. Throws `InvalidOperationException` if the connection string is invalid or the engine is unsupported. Throws `SqlException` or `IOException` on connection failure.

### `public async Task CloseAsync()`

Closes the underlying database connection asynchronously if it is open. If the connection is already closed, this method returns immediately without error. Does not roll back any active transaction; callers must explicitly commit or roll back before closing. Throws `InvalidOperationException` if the connection is in an inconsistent state.

### `public IDbTransaction BeginTransaction()`

Begins a new database transaction on the current connection. The transaction is created with the default isolation level. If no connection is open, throws `InvalidOperationException`. The returned transaction must be disposed by the caller to ensure proper cleanup. Nested calls reuse the existing transaction.

### `public async Task CommitAsync()`

Commits the current transaction asynchronously if one is active. If no transaction is active, throws `InvalidOperationException`. If the commit fails due to network or concurrency issues, throws `SqlException` or `IOException`. Automatically clears the transaction reference after completion.

### `public async Task RollbackAsync()`

Rolls back the current transaction asynchronously if one is active. If no transaction is active, returns without error. If the rollback fails due to network issues, throws `SqlException` or `IOException`. Automatically clears the transaction reference after completion.

### `public async Task<bool> TestConnectionAsync()`

Tests the database connection asynchronously without altering state. Returns `true` if the connection can be opened and a simple query succeeds; otherwise returns `false`. Does not modify the current connection state. May throw `SqlException` or `IOException` only if the test itself fails catastrophically (e.g., network down).

### `public async Task ExecuteInTransactionAsync(Action<IDbTransaction> action)`

Executes the provided action within a new or existing transaction asynchronously. If the action throws, the transaction is rolled back automatically. If the action completes successfully, the transaction is committed. The transaction is scoped to the call and disposed afterward. Throws `ArgumentNullException` if `action` is null. Propagates any exception thrown by `action`.

### `public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbTransaction, Task<T>> action)`

Executes the provided async function within a new or existing transaction asynchronously and returns the result. If the function throws, the transaction is rolled back automatically. If the function completes successfully, the transaction is committed. The transaction is scoped to the call and disposed afterward. Throws `ArgumentNullException` if `action` is null. Propagates any exception thrown by `action`.

### `public async Task<int> ExecuteAsync(string command, params object[] parameters)`

Executes a parameterized SQL command asynchronously and returns the number of rows affected. The command is executed within the current transaction if one is active; otherwise, it runs in autocommit mode. Throws `ArgumentNullException` if `command` is null. Throws `SqlException` or `IOException` on execution failure.

### `public async Task<T?> QueryFirstAsync<T>(string query, params object[] parameters)`

Executes a parameterized SQL query asynchronously and returns the first row mapped to type `T`. Returns `null` if no rows are returned. The query runs within the current transaction if one is active; otherwise, it runs in autocommit mode. Throws `ArgumentNullException` if `query` is null. Throws `SqlException` or `IOException` on execution failure. Throws `InvalidCastException` if the result cannot be mapped to `T`.

### `public async Task<object?> QueryScalarAsync(string query, params object[] parameters)`

Executes a parameterized SQL query asynchronously and returns the value of the first column of the first row. Returns `null` if no rows are returned. The query runs within the current transaction if one is active; otherwise, it runs in autocommit mode. Throws `ArgumentNullException` if `query` is null. Throws `SqlException` or `IOException` on execution failure.

### `public string GetDatabaseEngine()`

Returns a string identifying the database engine (e.g., "SQL Server", "PostgreSQL"). The value is determined at construction from the connection string. Never returns null.

### `public string GetMaskedConnectionString()`

Returns a sanitized version of the connection string with credentials redacted. Useful for logging without exposing sensitive information. Never returns null.

### `public void Dispose()`

Releases all resources associated with the `DatabaseContext`, including any active transaction and connection. If a transaction is active, it is rolled back implicitly. If the connection is open, it is closed. Subsequent calls to methods like `OpenAsync` will fail until a new instance is created. Safe to call multiple times.

### `public SqlServerConnection public bool IsOpen`

Exposes the underlying `SqlServerConnection` used by the context. Modifying this connection externally may lead to undefined behavior. The `IsOpen` property returns `true` if the connection is currently open; otherwise returns `false`.

### `public Task OpenAsync`

Alias for the instance method `OpenAsync()`. Provided for compatibility with fluent-style APIs.

### `public Task CloseAsync`

Alias for the instance method `CloseAsync()`. Provided for compatibility with fluent-style APIs.

### `public Task<bool> TestConnectionAsync`

Alias for the instance method `TestConnectionAsync()`. Provided for compatibility with fluent-style APIs.

## Usage
