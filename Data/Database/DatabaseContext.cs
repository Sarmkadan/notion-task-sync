#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Database;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Database context providing centralized data access and transaction management.
/// Acts as the main entry point for all database operations.
/// </summary>
public class DatabaseContext : IDisposable
{
    private IDbConnection? _connection;
    private IDbTransaction? _currentTransaction;
    private readonly string _connectionString;
    private readonly string _databaseEngine;
    private readonly int _commandTimeout;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
    /// The connection is not opened until <see cref="OpenAsync"/> is called or <see cref="Connection"/> is first accessed.
    /// </summary>
    /// <param name="connectionString">The connection string used to open the database.</param>
    /// <param name="databaseEngine">The engine name: "SQLite" (default), "SqlServer", or "PostgreSQL". Matched case-insensitively.</param>
    /// <param name="commandTimeout">The command timeout, in seconds. Defaults to 30.</param>
    public DatabaseContext(string connectionString, string databaseEngine = "SQLite", int commandTimeout = 30)
    {
        _connectionString = connectionString;
        _databaseEngine = databaseEngine;
        _commandTimeout = commandTimeout;
    }

    /// <summary>
    /// Gets or creates the database connection.
    /// </summary>
    public IDbConnection Connection
    {
        get
        {
            ThrowIfDisposed();
            return _connection ??= CreateConnection();
        }
    }

    /// <summary>
    /// Gets a value indicating whether a transaction is currently active.
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction?.IsActive ?? false;

    /// <summary>
    /// Opens the database connection asynchronously.
    /// </summary>
    public async Task OpenAsync()
    {
        ThrowIfDisposed();

        if (_connection is null)
        {
            _connection = CreateConnection();
        }

        if (!_connection.IsOpen)
        {
            await _connection.OpenAsync();
        }
    }

    /// <summary>
    /// Closes the database connection.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_connection is not null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
    }

    /// <summary>
    /// Begins a new transaction on the current connection.
    /// </summary>
    /// <returns>The newly started transaction.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a transaction is already active.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the context has been disposed.</exception>
    public IDbTransaction BeginTransaction()
    {
        ThrowIfDisposed();

        if (_currentTransaction is not null)
            throw new InvalidOperationException("A transaction is already active");

        _currentTransaction = Connection.BeginTransaction();
        return _currentTransaction;
    }

    /// <summary>
    /// Commits the current transaction if one is active.
    /// </summary>
    public async Task CommitAsync()
    {
        if (_currentTransaction is not null && _currentTransaction.IsActive)
        {
            await _currentTransaction.CommitAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction if one is active.
    /// </summary>
    public async Task RollbackAsync()
    {
        if (_currentTransaction is not null && _currentTransaction.IsActive)
        {
            await _currentTransaction.RollbackAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Tests the database connection.
    /// </summary>
    /// <returns><c>true</c> if the connection test succeeds; otherwise <c>false</c>. Exceptions are swallowed.</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await Connection.TestConnectionAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope, auto-committing on success.
    /// The transaction is rolled back and the exception rethrown if the action fails.
    /// </summary>
    /// <param name="action">The asynchronous work to run inside the transaction.</param>
    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        var transaction = BeginTransaction();

        try
        {
            await action();
            await CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope and returns a result.
    /// The transaction is rolled back and the exception rethrown if the action fails.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the action.</typeparam>
    /// <param name="action">The asynchronous work to run inside the transaction.</param>
    /// <returns>The result produced by <paramref name="action"/>.</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        var transaction = BeginTransaction();

        try
        {
            var result = await action();
            await CommitAsync();
            return result;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    /// <summary>
    /// Executes a command and returns the number of affected rows.
    /// </summary>
    /// <param name="command">The command text to execute.</param>
    /// <param name="parameters">Optional command parameters.</param>
    /// <returns>The number of affected rows.</returns>
    public async Task<int> ExecuteAsync(string command, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteCommandAsync(command, parameters);
    }

    /// <summary>
    /// Executes a query and returns the first result.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="query">The query text to execute.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <returns>The first result, or the default value of <typeparamref name="T"/> if there is none.</returns>
    public async Task<T?> QueryFirstAsync<T>(string query, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteQueryAsync<T>(query, parameters);
    }

    /// <summary>
    /// Executes a scalar query and returns a single value.
    /// </summary>
    /// <param name="query">The query text to execute.</param>
    /// <param name="parameters">Optional query parameters.</param>
    /// <returns>The scalar value, or <c>null</c> if the query returns none.</returns>
    public async Task<object?> QueryScalarAsync(string query, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteScalarAsync(query, parameters);
    }

    /// <summary>
    /// Gets the current database engine being used.
    /// </summary>
    /// <returns>The engine name as passed to the constructor (for example, "SQLite").</returns>
    public string GetDatabaseEngine()
    {
        return _databaseEngine;
    }

    /// <summary>
    /// Gets the connection string with sensitive data masked.
    /// </summary>
    /// <returns>The masked connection string, or <c>"***"</c> if no connection has been created yet.</returns>
    public string GetMaskedConnectionString()
    {
        return _connection?.GetMaskedConnectionString() ?? "***";
    }

    /// <summary>
    /// Creates a new database connection instance.
    /// </summary>
    private IDbConnection CreateConnection()
    {
        return _databaseEngine.ToLower() switch
        {
            "sqlserver" => new SqlServerConnection(_connectionString, _commandTimeout),
            "sqlite" => new SqliteConnection(_connectionString, _commandTimeout),
            "postgresql" => new PostgresConnection(_connectionString, _commandTimeout),
            _ => throw new NotSupportedException($"Database engine '{_databaseEngine}' is not supported")
        };
    }

    /// <summary>
    /// Validates that the context has not been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException("DatabaseContext");
    }

    /// <summary>
    /// Disposes the database context and closes the connection.
    /// Calling this method more than once has no effect.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            _currentTransaction?.Dispose();
            _connection?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }

        _isDisposed = true;
    }
}

// Placeholder implementations for different database engines
internal class SqlServerConnection : IDbConnection
{
    private readonly string _connectionString;
    private readonly int _commandTimeout;

    public SqlServerConnection(string connectionString, int commandTimeout)
    {
        _connectionString = connectionString;
        _commandTimeout = commandTimeout;
    }

    public bool IsOpen { get; private set; }
    public int ConnectionTimeout => _commandTimeout;
    public string DatabaseEngine => "SQL Server";

    public Task OpenAsync() => Task.CompletedTask;
    public Task CloseAsync() => Task.CompletedTask;
    public Task<bool> TestConnectionAsync() => Task.FromResult(true);
    public IDbTransaction BeginTransaction() => new NullTransaction();
    public Task<int> ExecuteCommandAsync(string commandText, object? parameters = null) => Task.FromResult(0);
    public Task<T?> ExecuteQueryAsync<T>(string queryText, object? parameters = null) => Task.FromResult<T?>(default);
    public Task<object?> ExecuteScalarAsync(string queryText, object? parameters = null) => Task.FromResult<object?>(null);
    public string GetMaskedConnectionString() => "***";
    public void Dispose() { }
}

internal class SqliteConnection : IDbConnection
{
    private readonly string _connectionString;
    private readonly int _commandTimeout;

    public SqliteConnection(string connectionString, int commandTimeout)
    {
        _connectionString = connectionString;
        _commandTimeout = commandTimeout;
    }

    public bool IsOpen { get; private set; }
    public int ConnectionTimeout => _commandTimeout;
    public string DatabaseEngine => "SQLite";

    public Task OpenAsync() => Task.CompletedTask;
    public Task CloseAsync() => Task.CompletedTask;
    public Task<bool> TestConnectionAsync() => Task.FromResult(true);
    public IDbTransaction BeginTransaction() => new NullTransaction();
    public Task<int> ExecuteCommandAsync(string commandText, object? parameters = null) => Task.FromResult(0);
    public Task<T?> ExecuteQueryAsync<T>(string queryText, object? parameters = null) => Task.FromResult<T?>(default);
    public Task<object?> ExecuteScalarAsync(string queryText, object? parameters = null) => Task.FromResult<object?>(null);
    public string GetMaskedConnectionString() => "***";
    public void Dispose() { }
}

internal class PostgresConnection : IDbConnection
{
    private readonly string _connectionString;
    private readonly int _commandTimeout;

    public PostgresConnection(string connectionString, int commandTimeout)
    {
        _connectionString = connectionString;
        _commandTimeout = commandTimeout;
    }

    public bool IsOpen { get; private set; }
    public int ConnectionTimeout => _commandTimeout;
    public string DatabaseEngine => "PostgreSQL";

    public Task OpenAsync() => Task.CompletedTask;
    public Task CloseAsync() => Task.CompletedTask;
    public Task<bool> TestConnectionAsync() => Task.FromResult(true);
    public IDbTransaction BeginTransaction() => new NullTransaction();
    public Task<int> ExecuteCommandAsync(string commandText, object? parameters = null) => Task.FromResult(0);
    public Task<T?> ExecuteQueryAsync<T>(string queryText, object? parameters = null) => Task.FromResult<T?>(default);
    public Task<object?> ExecuteScalarAsync(string queryText, object? parameters = null) => Task.FromResult<object?>(null);
    public string GetMaskedConnectionString() => "***";
    public void Dispose() { }
}

internal class NullTransaction : IDbTransaction
{
    public bool IsActive { get; } = true;
    public Task CommitAsync() => Task.CompletedTask;
    public Task RollbackAsync() => Task.CompletedTask;
    public void Dispose() { }
}
