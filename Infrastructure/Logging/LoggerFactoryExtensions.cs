using System;
using Microsoft.Extensions.Logging;

namespace NotionTaskSync.Infrastructure.Logging
{
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Ensures the directory for log files exists, creating it if necessary.
        /// </summary>
        public static void EnsureLogDirectoryExists(this LoggerFactory factory)
        {
            var logFilePath = factory.GetLogFilePath();
            if (string.IsNullOrEmpty(logFilePath))
                return;

            var directory = System.IO.Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Rotates the log file and cleans up old logs with a default retention period of 30 days.
        /// </summary>
        public static void RotateAndCleanupLogs(this LoggerFactory factory, int daysToKeep = 30)
        {
            factory.RotateLogFile();
            factory.CleanupOldLogs(daysToKeep);
        }

        /// <summary>
        /// Validates the log path configuration and throws if invalid.
        /// </summary>
        public static void ValidateLogConfiguration(this LoggerFactory factory)
        {
            if (!factory.ValidateLogPath())
            {
                throw new InvalidOperationException("Log path configuration is invalid or inaccessible.");
            }
        }
    }
}
