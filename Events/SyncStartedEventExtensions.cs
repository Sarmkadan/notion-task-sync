using System;
using System.Globalization;
using NotionTaskSync.Events;

public static class SyncStartedEventExtensions
{
    /// <summary>
    /// Returns a human-readable string representation of the sync started event.
    /// </summary>
    public static string ToHumanReadableString(this SyncStartedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return $"Sync started for config {@event.SyncConfigId} at {@event.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Returns a dictionary containing the sync started event's properties.
    /// </summary>
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
    /// Returns a string representation of the sync started event's properties.
    /// </summary>
    public static string ToQueryString(this SyncStartedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return $"syncConfigId={@event.SyncConfigId}&databaseId={@event.DatabaseId}&startTime={@event.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
    }
}
