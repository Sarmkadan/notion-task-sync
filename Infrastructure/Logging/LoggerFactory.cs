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
public class LoggerFactory
{
    private readonly string? _logFilePath;
    private readonly LogLevel _minLogLevel;
    private readonly bool _enableConsole;
    private readonly bool _enableFile;

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
    public string? GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// Validates that the log directory is accessible and writable.
    /// </summary>
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
