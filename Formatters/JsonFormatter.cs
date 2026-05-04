// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotionTaskSync.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats tasks and other domain objects as JSON for API responses and file storage.
/// Handles serialization with consistent formatting and null handling strategies.
/// Critical for inter-system communication and data export/import operations.
/// </summary>
public class JsonFormatter
{
    private readonly JsonSerializerSettings _settings;
    private readonly ILogger<JsonFormatter> _logger;

    public JsonFormatter(ILogger<JsonFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON settings for consistency
        _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "o", // ISO 8601 format
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new Newtonsoft.Json.Converters.StringEnumConverter(),
            }
        };
    }

    /// <summary>
    /// Serializes a single task to JSON string.
    /// </summary>
    public string FormatTask(Task task)
    {
        try
        {
            return JsonConvert.SerializeObject(task, _settings);
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
    public string FormatTasks(List<Task> tasks)
    {
        try
        {
            return JsonConvert.SerializeObject(tasks, _settings);
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
    public string FormatSyncConfig(SyncConfig config)
    {
        try
        {
            return JsonConvert.SerializeObject(config, _settings);
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
    public string Format<T>(T obj)
    {
        try
        {
            return JsonConvert.SerializeObject(obj, _settings);
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
    public Task? DeserializeTask(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<Task>(json, _settings);
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
    public List<Task>? DeserializeTasks(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<Task>>(json, _settings);
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
    public T? Deserialize<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
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
    public bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonConvert.DeserializeObject(json);
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
    public string Minify(string json)
    {
        try
        {
            var obj = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
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
    public string PrettyPrint(string json)
    {
        try
        {
            var obj = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pretty-print JSON");
            return json;
        }
    }
}
