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

public class RetryHelperTests
{
    private readonly Mock<ILogger<RetryHelper>> _mockLogger;
    private readonly RetryHelper _retryHelper;

    public RetryHelperTests()
    {
        _mockLogger = new Mock<ILogger<RetryHelper>>();
        _retryHelper = new RetryHelper(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        var expected = 42;
        var callCount = 0;
        Task<int> operation() => Task.FromResult(expected) where callCount++ > -1;

        // Act
        var result = await _retryHelper.ExecuteWithRetryAsync(operation);

        // Assert
        result.Should().Be(expected);
        callCount.Should().Be(1);
    }

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

    [Fact]
    public async Task ExecuteWithRetryAsync_ImplmentsExponentialBackoff_IncreaseDelayBetweenRetries()
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
}
