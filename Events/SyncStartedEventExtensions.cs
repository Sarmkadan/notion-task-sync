using System;
using System.Globalization;
using NotionTaskSync.Events;

public static class SyncStartedEventExtensions
{
    /// <summary>
    /// Returns a human-readable string representation of the sync started event.
    /// </summary>
    /// <param name="@event">The sync started event to convert to a human-readable string.</param>
    /// <returns>A human-readable string representation of the sync started event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="@event"/> is <see langword="null"/>.</exception>
    public static string ToHumanReadableString(this SyncStartedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return $"Sync started for config {@event.SyncConfigId} at {@event.StartTime:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// Returns a dictionary containing the sync started event's properties.
    /// </summary>
    /// <param name="@event">The sync started event to convert to a dictionary.</param>
    /// <returns>A dictionary containing the sync started event's properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="@event"/> is <see langword="null"/>.</exception>
    public static Dictionary<string, object> ToDictionary(this SyncStartedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new Dictionary<string, object>
        {
            ["SyncConfigId"] = @event.SyncConfigId,
            ["DatabaseId"] = @event.DatabaseId,
            ["StartTime"] = @event.StartTime,
        };
    }

    /// <summary>
    /// Returns a string representation of the sync started event's properties in query string format.
    /// </summary>
    /// <param name="@event">The sync started event to convert to a query string.</param>
    /// <returns>A query string representation of the sync started event's properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="@event"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="SyncStartedEvent.SyncConfigId"/> or <see cref="SyncStartedEvent.DatabaseId"/> is <see langword="null"/> or empty.</exception>
    public static string ToQueryString(this SyncStartedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        ArgumentException.ThrowIfNullOrEmpty(@event.SyncConfigId, nameof(@event.SyncConfigId));
        ArgumentException.ThrowIfNullOrEmpty(@event.DatabaseId, nameof(@event.DatabaseId));

        return $"syncConfigId={Uri.EscapeDataString(@event.SyncConfigId)}&databaseId={Uri.EscapeDataString(@event.DatabaseId)}&startTime={@event.StartTime:yyyy-MM-dd HH:mm:ss}";
    }
}