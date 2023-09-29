#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Services;

using NotionTaskSync.Domain.Models;
using NotionTaskSync.Domain.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles reading and writing task files from the local file system.
/// Supports Markdown format for task storage with metadata headers.
/// </summary>
public class LocalFileService
{
    private readonly string _basePath;
    private const string FileExtension = ".md";

    public LocalFileService(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentNullException(nameof(basePath));

        _basePath = basePath;
    }

    /// <summary>
    /// Saves a task to a local Markdown file.
    /// Creates or overwrites the file with task metadata and content.
    /// </summary>
    /// <param name="task">The task to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async global::System.Threading.Tasks.Task SaveTaskAsync(Task task)
    {
        if (!task.Validate())
            throw new ValidationException("Invalid task cannot be saved");

        try
        {
            EnsureDirectoryExists();

            var fileName = SanitizeFileName(task.Title);
            var filePath = Path.Combine(_basePath, $"{fileName}{FileExtension}");

            var content = FormatTaskAsMarkdown(task);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            task.LocalFilePath = filePath;
        }
        catch (Exception ex)
        {
            throw new LocalFileException($"Failed to save task '{task.Title}': {ex.Message}", ex)
            {
                FilePath = task.LocalFilePath
            };
        }
    }

    /// <summary>
    /// Loads a task from a local file by its path.
    /// </summary>
    /// <param name="filePath">The path to the local Markdown file.</param>
    /// <returns>The loaded <see cref="Task"/>, or null if the file does not exist.</returns>
    public async Task<Task?> LoadTaskAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            if (!File.Exists(filePath))
                return null;

            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return ParseTaskFromMarkdown(content, filePath);
        }
        catch (Exception ex)
        {
            throw new LocalFileException($"Failed to load task from '{filePath}': {ex.Message}", ex)
            {
                FilePath = filePath
            };
        }
    }

    /// <summary>
    /// Loads all tasks from files in the base directory.
    /// </summary>
    public async Task<List<Task>> LoadAllTasksAsync()
    {
        var tasks = new List<Task>();

        try
        {
            if (!Directory.Exists(_basePath))
                return tasks;

            var files = Directory.GetFiles(_basePath, $"*{FileExtension}");

            foreach (var file in files)
            {
                try
                {
                    var task = await LoadTaskAsync(file);

                    if (task is not null)
                        tasks.Add(task);
                }
                catch
                {
                    // Skip files that fail to load
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            throw new LocalFileException($"Failed to load tasks from '{_basePath}': {ex.Message}", ex);
        }

        return tasks;
    }

    /// <summary>
    /// Deletes a task file from the local file system.
    /// </summary>
    public async global::System.Threading.Tasks.Task DeleteTaskAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await global::System.Threading.Tasks.Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new LocalFileException($"Failed to delete task file: {ex.Message}", ex)
            {
                FilePath = filePath
            };
        }
    }

    /// <summary>
    /// Backs up all task files to a backup directory.
    /// </summary>
    public virtual async Task<string> BackupTasksAsync(string backupDir)
    {
        try
        {
            if (!Directory.Exists(_basePath))
                return string.Empty;

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"tasks_backup_{timestamp}");

            Directory.CreateDirectory(backupPath);

            var files = Directory.GetFiles(_basePath, $"*{FileExtension}");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var backupFilePath = Path.Combine(backupPath, fileName);
                File.Copy(file, backupFilePath, overwrite: true);
            }

            await global::System.Threading.Tasks.Task.CompletedTask;
            return backupPath;
        }
        catch (Exception ex)
        {
            throw new LocalFileException($"Failed to backup tasks: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the last modified time of any task file.
    /// </summary>
    public DateTime GetLastModifiedTime()
    {
        try
        {
            if (!Directory.Exists(_basePath))
                return DateTime.MinValue;

            var files = Directory.GetFiles(_basePath, $"*{FileExtension}");

            if (files.Length == 0)
                return DateTime.MinValue;

            return files
                .Select(f => File.GetLastWriteTimeUtc(f))
                .OrderByDescending(t => t)
                .First();
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Counts the number of task files in the directory.
    /// </summary>
    public int CountTaskFiles()
    {
        try
        {
            if (!Directory.Exists(_basePath))
                return 0;

            return Directory.GetFiles(_basePath, $"*{FileExtension}").Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Formats a task as Markdown with metadata headers.
    /// </summary>
    private string FormatTaskAsMarkdown(Task task)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {task.Title}");
        sb.AppendLine();
        sb.AppendLine("## Metadata");
        sb.AppendLine($"- **ID**: {task.Id}");
        sb.AppendLine($"- **Status**: {task.Status}");
        sb.AppendLine($"- **Priority**: {task.Priority}");

        if (task.DueDate.HasValue)
            sb.AppendLine($"- **Due Date**: {task.DueDate:yyyy-MM-dd}");

        if (!string.IsNullOrEmpty(task.AssignedTo))
            sb.AppendLine($"- **Assigned To**: {task.AssignedTo}");

        if (!string.IsNullOrEmpty(task.NotionPageId))
            sb.AppendLine($"- **Notion Page**: {task.NotionPageId}");

        sb.AppendLine($"- **Created**: {task.CreatedAt:O}");
        sb.AppendLine($"- **Updated**: {task.UpdatedAt:O}");

        if (!string.IsNullOrEmpty(task.Tags))
            sb.AppendLine($"- **Tags**: {task.Tags}");

        sb.AppendLine();
        sb.AppendLine("## Description");
        sb.AppendLine(task.Description ?? "(No description)");

        return sb.ToString();
    }

    /// <summary>
    /// Parses a task from Markdown content.
    /// </summary>
    private Task ParseTaskFromMarkdown(string content, string filePath)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length == 0 || !lines[0].TrimStart().StartsWith("#"))
            throw new LocalFileException($"Malformed task markdown in '{filePath}': missing title heading")
            {
                FilePath = filePath
            };

        var task = new Task
        {
            Title = string.Empty,
            LocalFilePath = filePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Extract title from first line
        task.Title = lines[0].TrimStart('#').Trim();

        // Parse metadata
        var inMetadata = false;
        var inDescription = false;
        var descriptionLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.Contains("## Metadata"))
            {
                inMetadata = true;
                inDescription = false;
                continue;
            }

            if (line.Contains("## Description"))
            {
                inMetadata = false;
                inDescription = true;
                continue;
            }

            if (line.StartsWith("##"))
            {
                inMetadata = false;
                inDescription = false;
            }

            if (inMetadata && line.StartsWith("- **"))
            {
                ParseMetadataLine(task, line);
            }
            else if (inDescription)
            {
                descriptionLines.Add(line);
            }
        }

        var description = string.Join("\n", descriptionLines).Trim();
        task.Description = description == "(No description)" ? null : description;

        return task;
    }

    /// <summary>
    /// Parses a single metadata line from Markdown.
    /// </summary>
    private void ParseMetadataLine(Task task, string line)
    {
        if (line.Contains("**Status**:"))
        {
            var status = line.Split(':')[1].Trim();

            if (Enum.TryParse<TaskStatus>(status, out var parsedStatus))
                task.Status = parsedStatus;
        }
        else if (line.Contains("**Priority**:"))
        {
            var priority = line.Split(':')[1].Trim();

            if (int.TryParse(priority, out var parsedPriority))
                task.Priority = parsedPriority;
        }
        else if (line.Contains("**Due Date**:"))
        {
            var dueDate = line.Split(':')[1].Trim();

            if (DateTime.TryParse(dueDate, out var parsedDate))
                task.DueDate = parsedDate;
        }
        else if (line.Contains("**Assigned To**:"))
        {
            task.AssignedTo = line.Split(':')[1].Trim();
        }
        else if (line.Contains("**Notion Page**:"))
        {
            task.NotionPageId = line.Split(':')[1].Trim();
        }
        else if (line.Contains("**Tags**:"))
        {
            task.Tags = line.Split(':')[1].Trim();
        }
    }

    /// <summary>
    /// Sanitizes a string to be used as a file name.
    /// </summary>
    private string SanitizeFileName(string name)
    {
        // Combine the platform's invalid filename characters with a fixed set of
        // characters that are unsafe across filesystems (e.g. Windows-reserved
        // characters that Linux's GetInvalidFileNameChars() does not flag).
        var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            '\\', '/', ':', '*', '?', '"', '<', '>', '|'
        };

        var sanitized = new string(name
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "task" : sanitized;
    }

    /// <summary>
    /// Ensures the base directory exists, creating it if necessary.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <summary>
    /// Gets the base directory path.
    /// </summary>
    public string GetBasePath() => _basePath;
}
