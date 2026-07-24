#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats tasks and other domain objects as JSON for API responses and file storage.
/// Handles serialization with consistent formatting and null handling strategies.
/// Critical for inter-system communication and data export/import operations.
/// Uses source-generated JsonSerializerContext for improved performance in hot loops.
/// </summary>
public class JsonFormatter : IFormatter
{
    private readonly AppJsonSerializerContext _jsonContext;
    private readonly ILogger<JsonFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for error reporting. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public JsonFormatter(ILogger<JsonFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonContext = new AppJsonSerializerContext();
    }

    /// <summary>
    /// Serializes a single task to JSON string.
    /// </summary>
    /// <param name="task">Task to serialize. Must not be null.</param>
    /// <returns>JSON string representation of the task.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is null.</exception>
    public string FormatTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            return _jsonContext.Serialize(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize task {TaskId}", task.Id);
            throw;
        }
    }

    /// <summary>
    /// Serializes a collection of tasks to JSON array string.
    /// </summary>
    /// <param name="tasks">Collection of tasks to serialize. Must not be null.</param>
    /// <returns>JSON array string representation of the tasks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tasks"/> is null.</exception>
    public string FormatTasks(List<Task> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        try
        {
            return _jsonContext.Serialize(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize {TaskCount} tasks", tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// Serializes a sync configuration to JSON.
    /// Used for configuration export and backup purposes.
    /// </summary>
    /// <param name="config">Configuration to serialize. Must not be null.</param>
    /// <returns>JSON string representation of the configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public string FormatSyncConfig(SyncConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            return _jsonContext.Serialize(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize sync configuration");
            throw;
        }
    }

    /// <summary>
    /// Serializes arbitrary objects to JSON with consistent formatting.
    /// Generic method for formatting any serializable object.
    /// </summary>
    /// <param name="obj">Object to serialize. Must not be null.</param>
    /// <returns>JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    public string Format<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        try
        {
            return _jsonContext.Serialize(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize object of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Deserializes a JSON string back into a task object.
    /// </summary>
    /// <param name="json">JSON string to deserialize. Must not be null.</param>
    /// <returns>Deserialized task object, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public Task? DeserializeTask(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return _jsonContext.Deserialize<Task>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize task from JSON");
            return null;
        }
    }

    /// <summary>
    /// Deserializes a JSON array string into a collection of tasks.
    /// </summary>
    /// <param name="json">JSON array string to deserialize. Must not be null.</param>
    /// <returns>List of deserialized tasks, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public List<Task>? DeserializeTasks(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return _jsonContext.Deserialize<List<Task>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize tasks from JSON");
            return null;
        }
    }

    /// <summary>
    /// Deserializes arbitrary JSON into specified type.
    /// Generic method for deserialization of any type.
    /// </summary>
    /// <param name="json">JSON string to deserialize. Must not be null.</param>
    /// <returns>Deserialized object of type T, or default if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public T? Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return _jsonContext.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to type {Type}", typeof(T).Name);
            return default;
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON.
    /// Useful for input validation before processing.
    /// </summary>
    /// <param name="json">JSON string to validate. Must not be null.</param>
    /// <returns>True if valid JSON; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public bool IsValidJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Minifies JSON by removing whitespace and formatting.
    /// Used for reducing data transfer size in API responses.
    /// </summary>
    /// <param name="json">JSON string to minify. Must not be null.</param>
    /// <returns>Minified JSON string. Returns original JSON on error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public string Minify(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to minify JSON");
            return json;
        }
    }

    /// <summary>
    /// Pretty-prints JSON by expanding whitespace for readability.
    /// Used for logging and debugging purposes.
    /// </summary>
    /// <param name="json">JSON string to pretty-print. Must not be null.</param>
    /// <returns>Pretty-printed JSON string. Returns original JSON on error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public string PrettyPrint(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pretty-print JSON");
            return json;
        }
    }
}
