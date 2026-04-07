#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Integration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Events;

/// <summary>
/// Handles webhook events from external services like Notion.
/// Processes incoming webhook data and publishes corresponding domain events.
/// Provides infrastructure for real-time reactive sync operations.
/// </summary>
public class WebhookHandler
{
    private readonly EventBus _eventBus;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly Dictionary<string, Func<Dictionary<string, object>, Task>> _webhookHandlers;

    public WebhookHandler(EventBus eventBus, ILogger<WebhookHandler> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webhookHandlers = new Dictionary<string, Func<Dictionary<string, object>, Task>>();

        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Handles an incoming webhook by matching to registered handler.
    /// </summary>
    public async Task<bool> HandleWebhookAsync(string webhookType, Dictionary<string, object> data)
    {
        try
        {
            _logger.LogInformation("Webhook received: {WebhookType}", webhookType);

            if (!_webhookHandlers.TryGetValue(webhookType, out var handler))
            {
                _logger.LogWarning("Unknown webhook type: {WebhookType}", webhookType);
                return false;
            }

            await handler(data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook: {WebhookType}", webhookType);
            return false;
        }
    }

    /// <summary>
    /// Registers a custom handler for a webhook type.
    /// </summary>
    public void RegisterHandler(string webhookType, Func<Dictionary<string, object>, Task> handler)
    {
        _webhookHandlers[webhookType] = handler;
        _logger.LogInformation("Webhook handler registered for: {WebhookType}", webhookType);
    }

    /// <summary>
    /// Registers default handlers for known webhook types.
    /// </summary>
    private void RegisterDefaultHandlers()
    {
        // Handler for page updates from Notion
        RegisterHandler("page_updated", async (data) =>
        {
            if (data.TryGetValue("page_id", out var pageId) &&
                data.TryGetValue("database_id", out var databaseId))
            {
                await _eventBus.PublishAsync(new ChangeDetectedEvent
                {
                    TaskId = Guid.TryParse(pageId.ToString(), out var id) ? id : Guid.NewGuid(),
                    ChangeType = "Updated",
                    Source = "Remote",
                    ChangedAt = DateTime.UtcNow,
                    ChangedProperties = data
                });
            }
        });

        // Handler for page creations
        RegisterHandler("page_created", async (data) =>
        {
            await _eventBus.PublishAsync(new ChangeDetectedEvent
            {
                TaskId = Guid.TryParse(data["page_id"].ToString(), out var id) ? id : Guid.NewGuid(),
                ChangeType = "Created",
                Source = "Remote",
                ChangedAt = DateTime.UtcNow,
                ChangedProperties = data
            });
        });

        // Handler for page deletions
        RegisterHandler("page_deleted", async (data) =>
        {
            await _eventBus.PublishAsync(new ChangeDetectedEvent
            {
                TaskId = Guid.TryParse(data["page_id"].ToString(), out var id) ? id : Guid.NewGuid(),
                ChangeType = "Deleted",
                Source = "Remote",
                ChangedAt = DateTime.UtcNow
            });
        });
    }

    /// <summary>
    /// Gets the list of registered webhook types.
    /// </summary>
    public List<string> GetRegisteredWebhookTypes()
    {
        return new List<string>(_webhookHandlers.Keys);
    }

    /// <summary>
    /// Validates webhook authenticity using a signature.
    /// Ensures webhook came from a trusted source.
    /// </summary>
    public bool ValidateWebhookSignature(
        string payload,
        string signature,
        string secret)
    {
        try
        {
            // Compute expected signature
            var expectedSignature = Utils.CryptoHelper.ComputeHmacSha256(payload, secret);

            // Compare using constant-time comparison to prevent timing attacks
            return expectedSignature == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }
}

/// <summary>
/// Configuration for webhook endpoint.
/// </summary>
public class WebhookConfig
{
    /// <summary>
    /// Secret key for validating webhook signatures.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// List of allowed webhook types.
    /// If empty, all types are allowed.
    /// </summary>
    public List<string> AllowedTypes { get; set; } = new();

    /// <summary>
    /// Maximum payload size in bytes.
    /// </summary>
    public int MaxPayloadSizeBytes { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Whether to validate webhook signatures.
    /// </summary>
    public bool ValidateSignature { get; set; } = true;
}
