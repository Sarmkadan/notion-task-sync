#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Central event bus for publishing and subscribing to application events.
/// Implements publish-subscribe pattern for loose coupling between components.
/// Allows handlers to react to sync events without direct dependency on event sources.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes a handler to an event type.
    /// Handler will be called whenever an event of that type is published.
    /// </summary>
    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var eventType = typeof(T);

        lock (_subscribers)
        {
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }

            _subscribers[eventType].Add(handler);
            _logger.LogDebug("Subscriber registered for event type {EventType}", eventType.Name);
        }
    }

    /// <summary>
    /// Subscribes a synchronous handler to an event type.
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);

        lock (_subscribers)
        {
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }

            _subscribers[eventType].Add(handler);
            _logger.LogDebug("Sync subscriber registered for event type {EventType}", eventType.Name);
        }
    }

    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// Returns completed Task after all handlers have executed.
    /// </summary>
    public async Task PublishAsync<T>(T @event) where T : class
    {
        var eventType = typeof(T);

        List<Delegate>? handlers;
        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
            {
                _logger.LogDebug("No subscribers for event type {EventType}", eventType.Name);
                return;
            }

            // Create a copy to avoid modification during iteration
            handlers = handlers.ToList();
        }

        _logger.LogInformation("Publishing event {EventType} to {SubscriberCount} subscribers",
            eventType.Name, handlers.Count);

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Func<T, Task> asyncHandler)
                {
                    tasks.Add(asyncHandler(@event));
                }
                else if (handler is Action<T> syncHandler)
                {
                    syncHandler(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing subscriber for event type {EventType}",
                    eventType.Name);
            }
        }

        // Wait for all async handlers to complete
        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }

        _logger.LogDebug("Event {EventType} published to all subscribers", eventType.Name);
    }

    /// <summary>
    /// Publishes an event synchronously (blocking).
    /// All handlers execute in calling thread.
    /// </summary>
    public void Publish<T>(T @event) where T : class
    {
        var eventType = typeof(T);

        List<Delegate>? handlers;
        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
            {
                return;
            }

            handlers = handlers.ToList();
        }

        _logger.LogInformation("Publishing event (sync) {EventType} to {SubscriberCount} subscribers",
            eventType.Name, handlers.Count);

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Action<T> syncHandler)
                {
                    syncHandler(@event);
                }
                // Async handlers cannot be awaited in sync context, log warning
                else if (handler is Func<T, Task>)
                {
                    _logger.LogWarning("Async handler registered for sync publish of {EventType}",
                        eventType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing subscriber for event type {EventType}",
                    eventType.Name);
            }
        }
    }

    /// <summary>
    /// Gets the count of subscribers for a specific event type.
    /// </summary>
    public int GetSubscriberCount<T>() where T : class
    {
        lock (_subscribers)
        {
            var eventType = typeof(T);
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Removes all subscribers for a specific event type.
    /// </summary>
    public void UnsubscribeAll<T>() where T : class
    {
        lock (_subscribers)
        {
            var eventType = typeof(T);
            if (_subscribers.Remove(eventType))
            {
                _logger.LogInformation("All subscribers removed for event type {EventType}",
                    eventType.Name);
            }
        }
    }

    /// <summary>
    /// Clears all subscribers from the event bus.
    /// </summary>
    public void Clear()
    {
        lock (_subscribers)
        {
            var count = _subscribers.Count;
            _subscribers.Clear();
            _logger.LogInformation("Event bus cleared ({Count} event types)", count);
        }
    }

    /// <summary>
    /// Gets diagnostic information about registered subscribers.
    /// </summary>
    public Dictionary<string, int> GetSubscriberInfo()
    {
        lock (_subscribers)
        {
            return _subscribers
                .ToDictionary(x => x.Key.Name, x => x.Value.Count);
        }
    }
}

/// <summary>
/// Base class for all application events.
/// Provides common metadata like timestamp.
/// </summary>
public abstract class ApplicationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string? Source { get; set; }
}
