#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Helper class for safe and reliable filesystem operations.
/// Provides utilities for reading, writing, and managing files with proper error handling.
/// Abstracts away filesystem complexity and makes code more testable.
/// </summary>
public sealed class FileSystemHelper
{
    private readonly ILogger<FileSystemHelper> _logger;

    public FileSystemHelper(ILogger<FileSystemHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// Safe to call multiple times; silently succeeds if directory already exists.
    /// </summary>
    public bool EnsureDirectoryExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Safely reads a file's content with proper error handling and encoding detection.
    /// Returns null if the file doesn't exist or cannot be read.
    /// </summary>
    public async Task<string?> ReadFileAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("File not found: {Path}", path);
                return null;
            }

            var content = await File.ReadAllTextAsync(path);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file: {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Safely writes content to a file with automatic directory creation.
    /// Overwrites existing file; use backups if preservation is needed.
    /// </summary>
    public async Task<bool> WriteFileAsync(string path, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureDirectoryExists(directory);
            }

            await File.WriteAllTextAsync(path, content);
            _logger.LogInformation("Wrote file: {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Safely appends content to a file, creating it if it doesn't exist.
    /// Useful for log files and append-only operations.
    /// </summary>
    public async Task<bool> AppendFileAsync(string path, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureDirectoryExists(directory);
            }

            await File.AppendAllTextAsync(path, content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append to file: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Safely deletes a file with logging.
    /// Doesn't throw if file doesn't exist.
    /// </summary>
    public bool DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("Deleted file: {Path}", path);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Safely deletes a directory and all its contents.
    /// Use with caution; includes recursive deletion.
    /// </summary>
    public bool DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                _logger.LogInformation("Deleted directory: {Path}", path);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete directory: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Copies a file with overwrite handling and error logging.
    /// Returns false if source doesn't exist or copy fails.
    /// </summary>
    public bool CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source file not found: {Path}", sourcePath);
                return false;
            }

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                EnsureDirectoryExists(destDir);
            }

            File.Copy(sourcePath, destinationPath, overwrite);
            _logger.LogInformation("Copied file from {Source} to {Destination}", sourcePath, destinationPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file from {Source} to {Destination}", sourcePath, destinationPath);
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// Returns -1 if the file doesn't exist or cannot be accessed.
    /// </summary>
    public long GetFileSize(string path)
    {
        try
        {
            if (!File.Exists(path))
                return -1;

            var fileInfo = new FileInfo(path);
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {Path}", path);
            return -1;
        }
    }

    /// <summary>
    /// Checks if a path is a directory.
    /// Returns false if the path doesn't exist.
    /// </summary>
    public bool IsDirectory(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a path is a file.
    /// Returns false if the path doesn't exist.
    /// </summary>
    public bool IsFile(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a file path to use forward slashes and removes redundant segments.
    /// Improves consistency across platforms (Windows/Unix).
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Convert to absolute path and normalize
        try
        {
            var fullPath = Path.GetFullPath(path);
            // Use forward slashes for consistency
            return fullPath.Replace("\\", "/");
        }
        catch
        {
            // If normalization fails, just replace backslashes
            return path.Replace("\\", "/");
        }
    }

    /// <summary>
    /// Gets the last modified time of a file.
    /// Returns DateTime.MinValue if the file doesn't exist.
    /// </summary>
    public DateTime GetLastModifiedTime(string path)
    {
        try
        {
            if (!File.Exists(path))
                return DateTime.MinValue;

            return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last modified time for: {Path}", path);
            return DateTime.MinValue;
        }
    }
}
