// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotionTaskSync.Utils;

namespace NotionTaskSync.Tests;

public class RetryHelperTests
{
    private readonly Mock<ILogger<RetryHelper>> _loggerMock = new();
    private readonly RetryHelper _sut;

    public RetryHelperTests()
    {
        _sut = new RetryHelper(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RetryHelper(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessOnFirstAttempt_ReturnsResult()
    {
        var result = await _sut.ExecuteWithRetryAsync(() => Task.FromResult(42), maxRetries: 3, initialDelayMs: 1);
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ZeroMaxRetries_ThrowsArgumentException()
    {
        var act = async () => await _sut.ExecuteWithRetryAsync(() => Task.FromResult(1), maxRetries: 0);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FailsAllAttempts_ThrowsLastException()
    {
        var attempt = 0;
        var act = async () => await _sut.ExecuteWithRetryAsync<int>(
            () =>
            {
                attempt++;
                throw new InvalidOperationException($"Fail #{attempt}");
            },
            maxRetries: 2,
            initialDelayMs: 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempt.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SucceedsOnSecondAttempt_ReturnsResult()
    {
        var attempt = 0;
        var result = await _sut.ExecuteWithRetryAsync(
            () =>
            {
                attempt++;
                if (attempt < 2) throw new Exception("transient");
                return Task.FromResult("success");
            },
            maxRetries: 3,
            initialDelayMs: 1);

        result.Should().Be("success");
        attempt.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_SkipsNonRetryable()
    {
        var act = async () => await _sut.ExecuteWithRetryAsync<int>(
            () => throw new ArgumentException("bad arg"),
            shouldRetry: ex => ex is not ArgumentException,
            maxRetries: 3,
            initialDelayMs: 1);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ExecuteWithRetry_Sync_SuccessOnFirstAttempt_ReturnsResult()
    {
        var result = _sut.ExecuteWithRetry(() => 99, maxRetries: 3, initialDelayMs: 1);
        result.Should().Be(99);
    }

    [Fact]
    public void ExecuteWithRetry_Sync_ZeroMaxRetries_ThrowsArgumentException()
    {
        var act = () => _sut.ExecuteWithRetry(() => 1, maxRetries: 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_SuccessfulOperation_ReturnsResult()
    {
        var (result, success) = await _sut.ExecuteWithCircuitBreakerAsync(() => Task.FromResult(42));
        success.Should().BeTrue();
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_FailedOperation_ReturnsFailure()
    {
        var (result, success) = await _sut.ExecuteWithCircuitBreakerAsync<int>(
            () => throw new Exception("boom"));
        success.Should().BeFalse();
        result.Should().Be(default);
    }
}
