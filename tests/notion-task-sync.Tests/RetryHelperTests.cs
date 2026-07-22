#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Tests;

using NotionTaskSync.Utils;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Contains unit tests for the <see cref="RetryHelper"/> class.
/// Tests various retry mechanisms including exponential backoff, circuit breaker patterns,
/// and custom retry predicates to ensure resilient operation execution.
/// </summary>
public class RetryHelperTests
{
    /// <summary>
    /// Mock logger instance for testing logging behavior in retry operations.
    /// </summary>
    private readonly Mock<ILogger<RetryHelper>> _mockLogger;

    /// <summary>
    /// Instance of <see cref="RetryHelper"/> being tested.
    /// </summary>
    private readonly RetryHelper _retryHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryHelperTests"/> class.
    /// Sets up mock logger and creates a new <see cref="RetryHelper"/> instance for testing.
    /// </summary>
    public RetryHelperTests()
    {
        _mockLogger = new Mock<ILogger<RetryHelper>>();
        _retryHelper = new RetryHelper(_mockLogger.Object);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> successfully returns the result
    /// on the first attempt when the operation succeeds immediately without requiring retries.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        var expected = 42;
        var callCount = 0;
        Task<int> operation()
        {
            callCount++;
            return Task.FromResult(expected);
        }

        // Act
        var result = await _retryHelper.ExecuteWithRetryAsync(operation);

        // Assert
        result.Should().Be(expected);
        callCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> retries the operation
    /// when it initially fails, then successfully returns the result on a subsequent attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationFailsInitiallyThenSucceeds_ReturnsResult()
    {
        // Arrange
        var expected = "success";
        var attemptCount = 0;

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("Temporary failure");
            return await Task.FromResult(expected);
        }

        // Act
        var result = await _retryHelper.ExecuteWithRetryAsync(operation, maxRetries: 3, initialDelayMs: 1);

        // Assert
        result.Should().Be(expected);
        attemptCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> throws the original exception
    /// when the operation fails on all retry attempts and exceeds the maximum retry count.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var testException = new InvalidOperationException("Persistent failure");

        async Task<string> operation()
        {
            throw testException;
        }

        // Act & Assert
        await _retryHelper.Invoking(r => r.ExecuteWithRetryAsync(operation, maxRetries: 2, initialDelayMs: 1))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Persistent failure");
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> throws an <see cref="ArgumentException"/>
    /// when the maxRetries parameter is set to 0 or a negative value.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WithInvalidMaxRetries_ThrowsArgumentException()
    {
        // Arrange
        async Task<int> operation() => await Task.FromResult(42);

        // Act & Assert
        await _retryHelper.Invoking(r => r.ExecuteWithRetryAsync(operation, maxRetries: 0, initialDelayMs: 1))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("maxRetries");
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> respects a custom retry predicate,
    /// retrying only for exceptions that match the predicate and throwing non-matching exceptions immediately.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_RetriesOnlyForRetryableExceptions()
    {
        // Arrange
        var attemptCount = 0;
        var retryableException = new TimeoutException("Timeout");
        var nonRetryableException = new InvalidOperationException("Invalid operation");

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount == 1)
                throw retryableException;
            throw nonRetryableException;
        }

        bool shouldRetry(Exception ex) => ex is TimeoutException;

        // Act & Assert
        await _retryHelper.Invoking(r => r.ExecuteWithRetryAsync(operation, shouldRetry, maxRetries: 3, initialDelayMs: 1))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid operation");

        attemptCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> successfully returns the result
    /// when a retryable exception occurs but is eventually resolved by a subsequent attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_SucceedsWhenRetryableExceptionResolved()
    {
        // Arrange
        var attemptCount = 0;
        var expected = "success";

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new TimeoutException("Timeout");
            return await Task.FromResult(expected);
        }

        bool shouldRetry(Exception ex) => ex is TimeoutException;

        // Act
        var result = await _retryHelper.ExecuteWithRetryAsync(operation, shouldRetry, maxRetries: 3, initialDelayMs: 1);

        // Assert
        result.Should().Be(expected);
        attemptCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetry{T}"/> successfully returns the result
    /// on the first attempt when the synchronous operation succeeds immediately without requiring retries.
    /// </summary>
    [Fact]
    public void ExecuteWithRetry_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        var expected = 99;
        var callCount = 0;

        int operation()
        {
            callCount++;
            return expected;
        }

        // Act
        var result = _retryHelper.ExecuteWithRetry(operation, maxRetries: 3, initialDelayMs: 1);

        // Assert
        result.Should().Be(expected);
        callCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetry{T}"/> retries the synchronous operation
    /// when it initially fails, then successfully returns the result on a subsequent attempt.
    /// </summary>
    [Fact]
    public void ExecuteWithRetry_WhenOperationFailsInitiallyThenSucceeds_ReturnsResult()
    {
        // Arrange
        var expected = "result";
        var attemptCount = 0;

        string operation()
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new IOException("File not found");
            return expected;
        }

        // Act
        var result = _retryHelper.ExecuteWithRetry(operation, maxRetries: 3, initialDelayMs: 1);

        // Assert
        result.Should().Be(expected);
        attemptCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetry{T}"/> throws the original exception
    /// when the synchronous operation fails on all retry attempts and exceeds the maximum retry count.
    /// </summary>
    [Fact]
    public void ExecuteWithRetry_WhenOperationExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var testException = new IOException("Persistent IO failure");

        string operation()
        {
            throw testException;
        }

        // Act & Assert
        _retryHelper.Invoking(r => r.ExecuteWithRetry(operation, maxRetries: 2, initialDelayMs: 1))
            .Should()
            .Throw<IOException>()
            .WithMessage("Persistent IO failure");
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetry{T}"/> throws an <see cref="ArgumentException"/>
    /// when the maxRetries parameter is set to 0 or a negative value for synchronous operations.
    /// </summary>
    [Fact]
    public void ExecuteWithRetry_WithInvalidMaxRetries_ThrowsArgumentException()
    {
        // Arrange
        int operation() => 42;

        // Act & Assert
        _retryHelper.Invoking(r => r.ExecuteWithRetry(operation, maxRetries: 0, initialDelayMs: 1))
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("maxRetries");
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithCircuitBreakerAsync{T}"/> successfully returns the result
    /// when the operation succeeds without any failures.
    /// </summary>
    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WhenOperationSucceeds_ReturnsSuccessTrue()
    {
        // Arrange
        var expected = "result";

        async Task<string> operation()
        {
            return await Task.FromResult(expected);
        }

        // Act
        var (result, success) = await _retryHelper.ExecuteWithCircuitBreakerAsync(operation);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithCircuitBreakerAsync{T}"/> returns success false
    /// when the operation fails and the circuit breaker threshold is exceeded.
    /// </summary>
    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_WhenOperationFails_ReturnsSuccessFalse()
    {
        // Arrange
        async Task<string> operation()
        {
            throw new InvalidOperationException("Operation failed");
        }

        // Act
        var (result, success) = await _retryHelper.ExecuteWithCircuitBreakerAsync(operation, maxFailures: 5);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> logs a warning message
    /// when a retryable failure occurs during operation execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_LogsWarningOnRetryableFailure()
    {
        // Arrange
        var attemptCount = 0;

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new TimeoutException("Temporary timeout");
            return await Task.FromResult("success");
        }

        // Act
        await _retryHelper.ExecuteWithRetryAsync(operation, maxRetries: 3, initialDelayMs: 1);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> logs an error message
    /// when the operation fails on all retry attempts and the maximum retry count is exceeded.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_LogsErrorAfterMaxRetriesExceeded()
    {
        // Arrange
        async Task<string> operation()
        {
            throw new InvalidOperationException("Persistent failure");
        }

        // Act & Assert
        await _retryHelper.Invoking(r => r.ExecuteWithRetryAsync(operation, maxRetries: 2, initialDelayMs: 1))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> implements exponential backoff
    /// by increasing the delay between retry attempts according to the configured parameters.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_ImplementsExponentialBackoff_IncreaseDelayBetweenRetries()
    {
        // Arrange
        var attemptCount = 0;
        var startTime = DateTime.UtcNow;

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new TimeoutException("Timeout");
            return await Task.FromResult("success");
        }

        // Act
        await _retryHelper.ExecuteWithRetryAsync(operation, maxRetries: 3, initialDelayMs: 50);

        var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert - should wait at least 50ms between retries (with jitter) but less than 10 seconds
        elapsedMs.Should().BeGreaterThanOrEqualTo(50);
        elapsedMs.Should().BeLessThan(10000);
        attemptCount.Should().Be(3);
    }


    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetryAsync{T}"/> implements exponential backoff
    /// with growing delays between retry attempts.
    /// </summary>
    [Fact]
    public async Task ExecuteWithRetryAsync_ImplementsExponentialBackoff_GrowsDelaysBetweenRetries()
    {
        // Arrange
        var attemptCount = 0;
        var delayTimes = new List<int>();
        var startTime = DateTime.UtcNow;

        async Task<string> operation()
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                // Record the delay that just happened (after attempts 1 and 2)
                if (attemptCount > 1)
                {
                    var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    delayTimes.Add(elapsed);
                }
                startTime = DateTime.UtcNow;
                throw new TimeoutException("Timeout");
            }
            return await Task.FromResult("success");
        }

        // Act
        await _retryHelper.ExecuteWithRetryAsync(operation, maxRetries: 5, initialDelayMs: 50);

        // Assert - delays should grow exponentially (approximately doubling each time)
        // We get 2 delays for 4 attempts (between 1-2 and 2-3)
        delayTimes.Count.Should().Be(2); // 2 delays between 3 retry attempts
        delayTimes[0].Should().BeGreaterThanOrEqualTo(50); // First delay >= initialDelayMs
        delayTimes[1].Should().BeGreaterThan((int)(delayTimes[0] * 1.5)); // Second delay > first delay
    }

    /// <summary>
    /// Tests that <see cref="RetryHelper.ExecuteWithRetry{T}"/> implements exponential backoff
    /// with growing delays between retry attempts for synchronous operations.
    /// </summary>
    [Fact]
    public void ExecuteWithRetry_ImplementsExponentialBackoff_GrowsDelaysBetweenRetries()
    {
        // Arrange
        var attemptCount = 0;
        var delayTimes = new List<int>();
        var startTime = DateTime.UtcNow;

        string operation()
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                // Record the delay that just happened (after attempts 1 and 2)
                if (attemptCount > 1)
                {
                    var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    delayTimes.Add(elapsed);
                }
                startTime = DateTime.UtcNow;
                throw new IOException("File not found");
            }
            return "success";
        }

        // Act
        _retryHelper.ExecuteWithRetry(operation, maxRetries: 5, initialDelayMs: 50);

        // Assert - delays should grow exponentially (approximately doubling each time)
        // We get 2 delays for 4 attempts (between 1-2 and 2-3)
        delayTimes.Count.Should().Be(2); // 2 delays between 3 retry attempts
        delayTimes[0].Should().BeGreaterThanOrEqualTo(50); // First delay >= initialDelayMs
        delayTimes[1].Should().BeGreaterThan((int)(delayTimes[0] * 1.5)); // Second delay > first delay
    }
}
