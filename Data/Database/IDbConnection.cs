#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Data.Database;

using System;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for database connection management.
/// Supports different database backends through a common interface.
/// </summary>
public interface IDbConnection : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the connection is open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Opens the database connection asynchronously.
    /// </summary>
    Task OpenAsync();

    /// <summary>
    /// Closes the database connection.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    /// Tests the connection to ensure the database is accessible.
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    IDbTransaction BeginTransaction();

    /// <summary>
    /// Executes a SQL command and returns the number of affected rows.
    /// </summary>
    Task<int> ExecuteCommandAsync(string commandText, object? parameters = null);

    /// <summary>
    /// Executes a query and returns the results.
    /// </summary>
    Task<T?> ExecuteQueryAsync<T>(string queryText, object? parameters = null);

    /// <summary>
    /// Executes a scalar query and returns a single value.
    /// </summary>
    Task<object?> ExecuteScalarAsync(string queryText, object? parameters = null);

    /// <summary>
    /// Gets the connection string (masked for security).
    /// </summary>
    string GetMaskedConnectionString();

    /// <summary>
    /// Gets the current connection timeout in seconds.
    /// </summary>
    int ConnectionTimeout { get; }

    /// <summary>
    /// Gets the name of the database engine (SQL Server, SQLite, etc).
    /// </summary>
    string DatabaseEngine { get; }
}

/// <summary>
/// Represents a database transaction for atomic operations.
/// </summary>
public interface IDbTransaction : IDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    Task RollbackAsync();

    /// <summary>
    /// Gets a value indicating whether the transaction is active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Exception raised for database connection errors.
/// </summary>
public class DbConnectionException : Exception
{
    public DbConnectionException(string message) : base(message) { }
    public DbConnectionException(string message, Exception inner) : base(message, inner) { }

    public string? ConnectionString { get; set; }
    public string? DatabaseName { get; set; }
}
