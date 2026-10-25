#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Logging;

using Microsoft.Extensions.Logging;
using System;
using System.IO;

/// <summary>
/// Factory for creating and configuring loggers for the application.
/// Handles both console and file logging setup.
/// </summary>
/// <remarks>
/// File output is currently a placeholder: <see cref="FileLoggerExtensions.AddFile"/> does not write
/// to disk yet, so enabling file logging only ensures the log directory exists.
/// </remarks>
public class LoggerFactory
{
    private readonly string? _logFilePath;
    private readonly LogLevel _minLogLevel;
    private readonly bool _enableConsole;
    private readonly bool _enableFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerFactory"/> class.
    /// </summary>
    /// <param name="logFilePath">Path to the log file. Used only when file logging is enabled.</param>
    /// <param name="minLogLevel">The minimum level of messages that loggers will emit.</param>
    /// <param name="enableConsole">When <c>true</c>, loggers write to the console.</param>
    /// <param name="enableFile">When <c>true</c> and <paramref name="logFilePath"/> is set, file logging is enabled.</param>
    public LoggerFactory(string? logFilePath = null, LogLevel minLogLevel = LogLevel.Information,
        bool enableConsole = true, bool enableFile = false)
    {
        _logFilePath = logFilePath;
        _minLogLevel = minLogLevel;
        _enableConsole = enableConsole;
        _enableFile = enableFile;
    }

    /// <summary>
    /// Creates a configured logger for a specific class.
    /// </summary>
    /// <typeparam name="T">The type whose name is used as the logger category.</typeparam>
    /// <returns>An <see cref="ILogger"/> for <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// Each call builds a new logging factory. When file logging is enabled, the log directory is created if it does not exist.
    /// </remarks>
    public ILogger CreateLogger<T>()
    {
        var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            if (_enableConsole)
                builder.AddConsole();
            builder.SetMinimumLevel(_minLogLevel);
        });

        if (_enableFile && !string.IsNullOrEmpty(_logFilePath))
        {
            // Ensure log directory exists
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            factory.AddFile(_logFilePath);
        }

        return factory.CreateLogger<T>();
    }

    /// <summary>
    /// Gets the configured log file path.
    /// </summary>
    /// <returns>The log file path passed to the constructor, or <c>null</c> if none was set.</returns>
    public string? GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// Validates that the log directory is accessible and writable.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the log directory exists or can be created and accepts writes;
    /// <c>true</c> when no log file path is set and file logging is disabled; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Creates the log directory if it is missing. Writes and then deletes a temporary <c>.test</c> file to check permissions.
    /// </remarks>
    public bool ValidateLogPath()
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return !_enableFile;

        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath);

            if (string.IsNullOrEmpty(logDir))
                return false;

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Test write permissions
            using (var stream = File.Create(Path.Combine(logDir, ".test")))
            {
                stream.WriteByte(0);
            }

            File.Delete(Path.Combine(logDir, ".test"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Rotates log files if they exceed a maximum size.
    /// </summary>
    /// <param name="maxSizeBytes">The size in bytes above which the log file is archived. Defaults to 10 MB.</param>
    /// <remarks>
    /// The current log file is moved to <c>{name}.{yyyyMMdd_HHmmss}.log</c> in the same directory (UTC timestamp).
    /// Does nothing if no log file path is set or the file does not exist.
    /// </remarks>
    public void RotateLogFile(long maxSizeBytes = 10485760) // 10MB default
    {
        if (string.IsNullOrEmpty(_logFilePath) || !File.Exists(_logFilePath))
            return;

        var fileInfo = new FileInfo(_logFilePath);

        if (fileInfo.Length > maxSizeBytes)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var archiveName = Path.Combine(
                Path.GetDirectoryName(_logFilePath) ?? ".",
                $"{Path.GetFileNameWithoutExtension(_logFilePath)}.{timestamp}.log");

            File.Move(_logFilePath, archiveName);
        }
    }

    /// <summary>
    /// Cleans up old log files based on retention policy.
    /// </summary>
    /// <param name="retentionDays">The number of days to keep archived log files. Defaults to 30.</param>
    /// <remarks>
    /// Deletes archived files matching <c>{name}.*.log</c> in the log directory whose last write time (UTC)
    /// is older than the retention period. The active log file is not matched by this pattern.
    /// Errors are ignored.
    /// </remarks>
    public void CleanupOldLogs(int retentionDays = 30)
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return;

        var logDir = Path.GetDirectoryName(_logFilePath);

        if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
            return;

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var pattern = $"{Path.GetFileNameWithoutExtension(_logFilePath)}.*.log";

        try
        {
            foreach (var file in Directory.GetFiles(logDir, pattern))
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Silently ignore cleanup errors
        }
    }
}

/// <summary>
/// Extension methods for ILoggerFactory to support file logging.
/// </summary>
public static class FileLoggerExtensions
{
    public static ILoggerFactory AddFile(this ILoggerFactory factory, string filePath)
    {
        // This is a simplified implementation
        // In production, you'd use a full logging provider like Serilog
        return factory;
    }
}
