using System;
using Microsoft.Extensions.Logging;

namespace NotionTaskSync.Infrastructure.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILoggerFactory"/> to support file logging operations.
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Ensures the directory for log files exists, creating it if necessary.
        /// </summary>
        /// <param name="factory">The logger factory instance.</param>
        /// <param name="logFilePath">The log file path to ensure directory exists for.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logFilePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="logFilePath"/> is empty or contains only whitespace.</exception>
        public static void EnsureLogDirectoryExists(this ILoggerFactory factory, string logFilePath)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            var directory = System.IO.Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Rotates the log file if it exceeds the specified maximum size and cleans up old logs.
        /// </summary>
        /// <param name="factory">The logger factory instance.</param>
        /// <param name="logFilePath">The path to the log file to rotate.</param>
        /// <param name="maxSizeBytes">Maximum log file size in bytes before rotation (default: 10MB).</param>
        /// <param name="retentionDays">Number of days to keep old log files (default: 30).</param>
        /// <exception cref="ArgumentNullException"><paramref name="logFilePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="logFilePath"/> is empty or contains only whitespace.</exception>
        public static void RotateAndCleanupLogs(this ILoggerFactory factory, string logFilePath,
            long maxSizeBytes = 10485760, int retentionDays = 30)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            RotateLogFile(factory, logFilePath, maxSizeBytes);
            CleanupOldLogs(factory, logFilePath, retentionDays);
        }

        /// <summary>
        /// Rotates the log file if it exceeds the specified maximum size.
        /// Creates a timestamped archive of the current log file and starts a new one.
        /// </summary>
        /// <param name="factory">The logger factory instance.</param>
        /// <param name="logFilePath">The path to the log file to rotate.</param>
        /// <param name="maxSizeBytes">Maximum log file size in bytes before rotation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logFilePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="logFilePath"/> is empty or contains only whitespace.</exception>
        public static void RotateLogFile(this ILoggerFactory factory, string logFilePath, long maxSizeBytes = 10485760)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            if (!System.IO.File.Exists(logFilePath))
            {
                return;
            }

            var fileInfo = new System.IO.FileInfo(logFilePath);
            if (fileInfo.Length <= maxSizeBytes)
            {
                return;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var archiveName = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(logFilePath) ?? ".",
                $"{System.IO.Path.GetFileNameWithoutExtension(logFilePath)}.{timestamp}.log"
            );

            try
            {
                System.IO.File.Move(logFilePath, archiveName);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                // Log rotation failed, continue with normal logging
                // This is not critical, so we don't throw
            }
        }

        /// <summary>
        /// Cleans up old log files based on retention policy.
        /// </summary>
        /// <param name="factory">The logger factory instance.</param>
        /// <param name="logFilePath">The path to the current log file.</param>
        /// <param name="retentionDays">Number of days to keep old log files.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logFilePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="logFilePath"/> is empty or contains only whitespace.</exception>
        public static void CleanupOldLogs(this ILoggerFactory factory, string logFilePath, int retentionDays = 30)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            var logDir = System.IO.Path.GetDirectoryName(logFilePath);
            if (string.IsNullOrEmpty(logDir) || !System.IO.Directory.Exists(logDir))
            {
                return;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var pattern = $"{System.IO.Path.GetFileNameWithoutExtension(logFilePath)}.*.log";

            try
            {
                foreach (var file in System.IO.Directory.GetFiles(logDir, pattern))
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
                        {
                            // Silently ignore cleanup errors to prevent logging failures
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Silently ignore directory access errors
            }
        }

        /// <summary>
        /// Validates that the log directory is accessible and writable.
        /// </summary>
        /// <param name="factory">The logger factory instance.</param>
        /// <param name="logFilePath">The log file path to validate.</param>
        /// <returns>True if the log path is valid and accessible; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="logFilePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="logFilePath"/> is empty or contains only whitespace.</exception>
        public static bool ValidateLogPath(this ILoggerFactory factory, string logFilePath)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            try
            {
                var logDir = System.IO.Path.GetDirectoryName(logFilePath);
                if (string.IsNullOrEmpty(logDir))
                {
                    return false;
                }

                if (!System.IO.Directory.Exists(logDir))
                {
                    return false;
                }

                // Test write permissions
                var testFilePath = System.IO.Path.Combine(logDir, $".test_{Guid.NewGuid()}");
                try
                {
                    using (var stream = System.IO.File.Create(testFilePath))
                    {
                        stream.WriteByte(0);
                    }

                    System.IO.File.Delete(testFilePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
