using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Extension methods for <see cref="CollaborationSessionOptions"/> that provide
/// validation, diagnostics and simple calculations based on the option values.
/// </summary>
namespace NotionTaskSync.Collaboration
{
    /// <summary>
    /// Provides additional functionality for <see cref="CollaborationSessionOptions"/>.
    /// </summary>
    public static class CollaborationSessionOptionsExtensions
    {
        /// <summary>
        /// Validates the option values and throws an <see cref="ArgumentException"/> if any
        /// value is out of the expected range.
        /// </summary>
        /// <param name="options">The options instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when any numeric or time‑span property does not satisfy its constraints.
        /// </exception>
        public static void EnsureValid(this CollaborationSessionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.MaxParticipantsPerSession <= 0)
                throw new ArgumentException("MaxParticipantsPerSession must be greater than zero.", nameof(options));

            if (options.OperationLogCapacity < 0)
                throw new ArgumentException("OperationLogCapacity cannot be negative.", nameof(options));

            if (options.MaxOperationsPerBatch <= 0)
                throw new ArgumentException("MaxOperationsPerBatch must be greater than zero.", nameof(options));

            if (options.IdleTimeout <= TimeSpan.Zero)
                throw new ArgumentException("IdleTimeout must be a positive time span.", nameof(options));

            if (options.HeartbeatInterval <= TimeSpan.Zero)
                throw new ArgumentException("HeartbeatInterval must be a positive time span.", nameof(options));
        }

        /// <summary>
        /// Returns a concise, human‑readable description of the current configuration.
        /// </summary>
        /// <param name="options">The options instance to describe.</param>
        /// <returns>A formatted string containing the most important option values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        public static string ToHumanReadableString(this CollaborationSessionOptions options) =>
            $"{nameof(CollaborationSessionOptions)}: " +
            $"MaxParticipants={options.MaxParticipantsPerSession.ToString(CultureInfo.InvariantCulture)}, " +
            $"LogCapacity={options.OperationLogCapacity.ToString(CultureInfo.InvariantCulture)}, " +
            $"BatchSize={options.MaxOperationsPerBatch.ToString(CultureInfo.InvariantCulture)}, " +
            $"IdleTimeout={options.IdleTimeout}, " +
            $"Heartbeat={options.HeartbeatInterval}, " +
            $"AutoTextMerge={options.AllowAutomaticTextMerge}, " +
            $"ConflictPolicy={options.ScalarConflictPolicy}, " +
            $"PersistLog={options.PersistOperationsToChangeLog}, " +
            $"ObserverEdits={options.AllowObserverEdits}, " +
            $"Validate={options.Validate}";

        /// <summary>
        /// Calculates the effective heartbeat interval taking an optional network latency into account.
        /// The effective interval is the larger of the configured <see cref="CollaborationSessionOptions.HeartbeatInterval"/>
        /// and the supplied <paramref name="networkLatency"/>.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <param name="networkLatency">
        /// Optional measured network latency. If <c>null</c>, only the configured interval is used.
        /// </param>
        /// <returns>The effective heartbeat <see cref="TimeSpan"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        public static TimeSpan GetEffectiveHeartbeatInterval(this CollaborationSessionOptions options, TimeSpan? networkLatency = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            return networkLatency.HasValue && networkLatency.Value > options.HeartbeatInterval
                ? networkLatency.Value
                : options.HeartbeatInterval;
        }

        /// <summary>
        /// Estimates the total memory footprint of the operation log based on an average operation size.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <param name="averageOperationSizeBytes">
        /// The average size of a single operation in bytes. Defaults to 256 bytes.
        /// </param>
        /// <returns>An estimated size in bytes as a <see cref="long"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="averageOperationSizeBytes"/> is less than or equal to zero.
        /// </exception>
        public static long EstimateOperationLogSizeBytes(this CollaborationSessionOptions options, int averageOperationSizeBytes = 256)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(averageOperationSizeBytes.ToString(CultureInfo.InvariantCulture));
            if (averageOperationSizeBytes <= 0)
                throw new ArgumentException("Average operation size must be greater than zero.", nameof(averageOperationSizeBytes));

            return (long)options.OperationLogCapacity * averageOperationSizeBytes;
        }
    }
}
