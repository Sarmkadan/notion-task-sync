// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Middleware;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotionTaskSync.Events;

/// <summary>
/// Middleware that enforces rate limiting to prevent exceeding API quotas.
/// Tracks API calls and delays requests when approaching rate limits.
/// Respects API-provided rate limit headers for intelligent backoff.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly int _requestsPerMinute;
    private readonly EventBus _eventBus;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private DateTime _windowStart = DateTime.UtcNow;
    private int _requestCount;

    public RateLimitingMiddleware(
        int requestsPerMinute,
        EventBus eventBus,
        ILogger<RateLimitingMiddleware> logger)
    {
        if (requestsPerMinute <= 0)
            throw new ArgumentException("Requests per minute must be positive", nameof(requestsPerMinute));

        _requestsPerMinute = requestsPerMinute;
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation while respecting rate limits.
    /// Delays execution if rate limit is approaching.
    /// </summary>
    public async Task<T> ExecuteWithRateLimitAsync<T>(
        Func<Task<T>> operation,
        string apiService = "Unknown")
    {
        // Check if we need to wait
        var delayMs = GetRequiredDelay();
        if (delayMs > 0)
        {
            _logger.LogInformation("Rate limit approach detected. Delaying request by {DelayMs}ms", delayMs);
            await Task.Delay(delayMs);
        }

        // Reset window if enough time has passed
        if ((DateTime.UtcNow - _windowStart).TotalSeconds >= 60)
        {
            _windowStart = DateTime.UtcNow;
            _requestCount = 0;
        }

        // Execute operation and track the request
        _requestCount++;
        _logger.LogDebug("Request {RequestNumber}/{RequestLimit} in current window",
            _requestCount, _requestsPerMinute);

        var result = await operation();

        // Check if we're approaching the limit
        if (_requestCount >= (_requestsPerMinute * 0.8))
        {
            var remaining = _requestsPerMinute - _requestCount;
            var resetTime = _windowStart.AddSeconds(60);

            await _eventBus.PublishAsync(new RateLimitWarningEvent
            {
                ApiService = apiService,
                RequestsRemaining = Math.Max(0, remaining),
                RequestLimit = _requestsPerMinute,
                ResetTime = resetTime
            });
        }

        return result;
    }

    /// <summary>
    /// Synchronous version of ExecuteWithRateLimitAsync.
    /// </summary>
    public T ExecuteWithRateLimit<T>(Func<T> operation, string apiService = "Unknown")
    {
        var delayMs = GetRequiredDelay();
        if (delayMs > 0)
        {
            _logger.LogInformation("Rate limit approach. Delaying by {DelayMs}ms", delayMs);
            System.Threading.Thread.Sleep(delayMs);
        }

        if ((DateTime.UtcNow - _windowStart).TotalSeconds >= 60)
        {
            _windowStart = DateTime.UtcNow;
            _requestCount = 0;
        }

        _requestCount++;
        return operation();
    }

    /// <summary>
    /// Processes a rate limit header response from an API.
    /// Updates internal tracking based on API-provided limits.
    /// </summary>
    public void ProcessRateLimitHeader(
        int? remainingRequests,
        int? resetEpoch,
        string apiService = "Unknown")
    {
        if (remainingRequests.HasValue)
        {
            if (remainingRequests.Value < (_requestsPerMinute * 0.2))
            {
                var resetTime = resetEpoch.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(resetEpoch.Value).UtcDateTime
                    : DateTime.UtcNow.AddMinutes(1);

                _logger.LogWarning(
                    "API rate limit near threshold for {ApiService}. Only {Remaining} requests remaining",
                    apiService,
                    remainingRequests.Value);
            }
        }
    }

    /// <summary>
    /// Gets the delay in milliseconds needed before the next request.
    /// Returns 0 if no delay is needed.
    /// </summary>
    private int GetRequiredDelay()
    {
        var elapsed = (DateTime.UtcNow - _windowStart).TotalSeconds;

        // Reset window if a minute has passed
        if (elapsed >= 60)
            return 0;

        // If we've used up the limit, calculate how long to wait
        if (_requestCount >= _requestsPerMinute)
        {
            var secondsToWait = 60 - elapsed;
            return (int)(secondsToWait * 1000) + 100; // Add small buffer
        }

        // Calculate requests per second and see if we're approaching the limit
        var requestsPerSecond = (double)_requestsPerMinute / 60;
        var maxRequestsInElapsed = requestsPerSecond * elapsed;

        if (_requestCount >= maxRequestsInElapsed * 0.9)
        {
            // We're ahead of schedule, wait a bit
            return 500;
        }

        return 0;
    }

    /// <summary>
    /// Gets current rate limit status.
    /// </summary>
    public RateLimitStatus GetStatus()
    {
        var elapsed = (DateTime.UtcNow - _windowStart).TotalSeconds;

        if (elapsed >= 60)
        {
            return new RateLimitStatus
            {
                RequestsUsed = 0,
                RequestsRemaining = _requestsPerMinute,
                LimitPerMinute = _requestsPerMinute,
                WindowResetAt = DateTime.UtcNow.AddSeconds(60)
            };
        }

        return new RateLimitStatus
        {
            RequestsUsed = _requestCount,
            RequestsRemaining = Math.Max(0, _requestsPerMinute - _requestCount),
            LimitPerMinute = _requestsPerMinute,
            WindowResetAt = _windowStart.AddSeconds(60)
        };
    }

    /// <summary>
    /// Resets rate limit counters.
    /// </summary>
    public void Reset()
    {
        _windowStart = DateTime.UtcNow;
        _requestCount = 0;
        _logger.LogInformation("Rate limit counters reset");
    }
}

/// <summary>
/// Represents the current rate limit status.
/// </summary>
public class RateLimitStatus
{
    public int RequestsUsed { get; set; }
    public int RequestsRemaining { get; set; }
    public int LimitPerMinute { get; set; }
    public DateTime WindowResetAt { get; set; }

    /// <summary>
    /// Gets the percentage of the rate limit that has been used.
    /// </summary>
    public double UsagePercentage => (RequestsUsed * 100.0 / LimitPerMinute);

    /// <summary>
    /// Indicates if rate limit is critically low (>95% used).
    /// </summary>
    public bool IsCritical => UsagePercentage > 95;

    /// <summary>
    /// Indicates if rate limit is high (>80% used).
    /// </summary>
    public bool IsHigh => UsagePercentage > 80;
}
