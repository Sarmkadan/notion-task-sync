// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Formatters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NotionTaskSync.Domain.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats tasks as CSV (Comma-Separated Values) for spreadsheet import/export.
/// Handles proper escaping of values containing commas, quotes, and line breaks.
/// Provides easy integration with Excel and other spreadsheet applications.
/// </summary>
public class CsvFormatter
{
    private readonly ILogger<CsvFormatter> _logger;

    public CsvFormatter(ILogger<CsvFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Formats a collection of tasks as CSV with header row.
    /// </summary>
    public string FormatTasks(List<Task> tasks)
    {
        try
        {
            var csv = new StringBuilder();

            // Write header
            csv.AppendLine(CsvHeader());

            // Write data rows
            foreach (var task in tasks)
            {
                csv.AppendLine(FormatTaskRow(task));
            }

            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format {TaskCount} tasks as CSV", tasks.Count);
            throw;
        }
    }

    /// <summary>
    /// Formats a single task as a CSV row.
    /// </summary>
    public string FormatTask(Task task)
    {
        try
        {
            var header = CsvHeader();
            var row = FormatTaskRow(task);
            return $"{header}\n{row}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format task {TaskId} as CSV", task.Id);
            throw;
        }
    }

    /// <summary>
    /// Parses a CSV string into task objects.
    /// Expects header row; returns list of parsed tasks.
    /// </summary>
    public List<Task> ParseTasks(string csv)
    {
        var tasks = new List<Task>();

        try
        {
            var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (lines.Length < 2)
            {
                _logger.LogWarning("CSV contains no data rows");
                return tasks;
            }

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var task = ParseTaskRow(line);
                if (task != null)
                    tasks.Add(task);
            }

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CSV into tasks");
            return tasks;
        }
    }

    /// <summary>
    /// Returns the CSV header row defining all task fields.
    /// </summary>
    private static string CsvHeader()
    {
        return "Id,Title,Description,Status,Priority,CreatedAt,UpdatedAt,DueDate,CompletedAt,AssignedTo,Tags,IsDeleted";
    }

    /// <summary>
    /// Formats a single task as a CSV row with proper escaping.
    /// Handles null values and special characters correctly.
    /// </summary>
    private static string FormatTaskRow(Task task)
    {
        return CsvEscape(task.Id.ToString()) + "," +
               CsvEscape(task.Title) + "," +
               CsvEscape(task.Description) + "," +
               CsvEscape(task.Status.ToString()) + "," +
               task.Priority + "," +
               CsvEscape(task.CreatedAt.ToString("o")) + "," +
               CsvEscape(task.UpdatedAt.ToString("o")) + "," +
               CsvEscape(task.DueDate?.ToString("o")) + "," +
               CsvEscape(task.CompletedAt?.ToString("o")) + "," +
               CsvEscape(task.AssignedTo) + "," +
               CsvEscape(task.Tags) + "," +
               task.IsDeleted;
    }

    /// <summary>
    /// Parses a single CSV row back into a Task object.
    /// Assumes fields are in the order defined by CsvHeader().
    /// </summary>
    private Task? ParseTaskRow(string line)
    {
        try
        {
            var values = CsvUnescape(line);

            if (values.Length < 12)
                return null;

            var task = new Task
            {
                Id = Guid.Parse(values[0]),
                Title = values[1],
                Description = values[2],
                Status = Enum.Parse<TaskStatus>(values[3]),
                Priority = int.Parse(values[4]),
                CreatedAt = DateTime.Parse(values[5]),
                UpdatedAt = DateTime.Parse(values[6]),
                DueDate = string.IsNullOrEmpty(values[7]) ? null : DateTime.Parse(values[7]),
                CompletedAt = string.IsNullOrEmpty(values[8]) ? null : DateTime.Parse(values[8]),
                AssignedTo = values[9],
                Tags = values[10],
                IsDeleted = bool.Parse(values[11])
            };

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse CSV row: {Line}", line);
            return null;
        }
    }

    /// <summary>
    /// Escapes a value for CSV by wrapping in quotes if it contains special characters.
    /// </summary>
    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Unescapes and splits a CSV line into individual values.
    /// Handles quoted values correctly.
    /// </summary>
    private static string[] CsvUnescape(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }
}
