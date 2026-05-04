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

        if (_connection == null)
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
        if (_connection != null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
    }

    /// <summary>
    /// Begins a new transaction if one is not already active.
    /// </summary>
    public IDbTransaction BeginTransaction()
    {
        ThrowIfDisposed();

        if (_currentTransaction != null)
            throw new InvalidOperationException("A transaction is already active");

        _currentTransaction = Connection.BeginTransaction();
        return _currentTransaction;
    }

    /// <summary>
    /// Commits the current transaction if one is active.
    /// </summary>
    public async Task CommitAsync()
    {
        if (_currentTransaction != null && _currentTransaction.IsActive)
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
        if (_currentTransaction != null && _currentTransaction.IsActive)
        {
            await _currentTransaction.RollbackAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Tests the database connection.
    /// </summary>
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
    /// </summary>
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
    /// </summary>
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
    public async Task<int> ExecuteAsync(string command, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteCommandAsync(command, parameters);
    }

    /// <summary>
    /// Executes a query and returns the first result.
    /// </summary>
    public async Task<T?> QueryFirstAsync<T>(string query, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteQueryAsync<T>(query, parameters);
    }

    /// <summary>
    /// Executes a scalar query and returns a single value.
    /// </summary>
    public async Task<object?> QueryScalarAsync(string query, object? parameters = null)
    {
        ThrowIfDisposed();
        return await Connection.ExecuteScalarAsync(query, parameters);
    }

    /// <summary>
    /// Gets the current database engine being used.
    /// </summary>
    public string GetDatabaseEngine()
    {
        return _databaseEngine;
    }

    /// <summary>
    /// Gets the connection string with sensitive data masked.
    /// </summary>
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
