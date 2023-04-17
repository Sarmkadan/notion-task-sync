// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Events.EventHandlers;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Formatters;

/// <summary>
/// Event handler that responds to conflict detected events.
/// Logs conflicts for tracking and potentially triggers notifications.
/// Provides detailed information about what caused the conflict for analysis.
/// </summary>
public class ConflictDetectedHandler
{
    private readonly ILogger<ConflictDetectedHandler> _logger;
    private readonly JsonFormatter _jsonFormatter;

    public ConflictDetectedHandler(
        ILogger<ConflictDetectedHandler> logger,
        JsonFormatter jsonFormatter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonFormatter = jsonFormatter ?? throw new ArgumentNullException(nameof(jsonFormatter));
    }

    /// <summary>
    /// Handles a conflict detected event.
    /// Logs the conflict and extracts useful diagnostic information.
    /// </summary>
    public async Task HandleAsync(ConflictDetectedEvent @event)
    {
        try
        {
            // Log the conflict at warning level
            _logger.LogWarning(
                "Conflict detected for task '{TaskTitle}' (ID: {TaskId}). " +
                "Type: {ConflictType}. Local modified: {LocalTime}, Remote modified: {RemoteTime}",
                @event.TaskTitle,
                @event.TaskId,
                @event.ConflictType,
                @event.LocalModifiedAt,
                @event.RemoteModifiedAt);

            // Log detailed conflict information
            LogDetailedConflictInfo(@event);

            // Could trigger notifications here (email, Slack, etc.)
            await NotifyConflictAsync(@event);

            _logger.LogDebug("Conflict handler processed event {EventId}", @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling conflict detected event");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Logs detailed information about the conflicting property values.
    /// </summary>
    private void LogDetailedConflictInfo(ConflictDetectedEvent @event)
    {
        if (@event.LocalValues.Count > 0)
        {
            _logger.LogDebug("Local values for conflict: {LocalValues}",
                _jsonFormatter.Format(@event.LocalValues));
        }

        if (@event.RemoteValues.Count > 0)
        {
            _logger.LogDebug("Remote values for conflict: {RemoteValues}",
                _jsonFormatter.Format(@event.RemoteValues));
        }
    }

    /// <summary>
    /// Sends a notification about the conflict.
    /// Placeholder for integration with notification services.
    /// </summary>
    private async Task NotifyConflictAsync(ConflictDetectedEvent @event)
    {
        // TODO: Implement actual notification (email, Slack, webhook, etc.)
        await Task.CompletedTask;
    }
}
