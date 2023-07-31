# RetryHelperTests

The `RetryHelperTests` class contains the comprehensive suite of unit tests for the `RetryHelper` utility within the `notion-task-sync` project. These tests are designed to validate the correctness, robustness, and error-handling capabilities of asynchronous and synchronous retry mechanisms, circuit breaker patterns, logging behaviors, and exponential backoff strategies implemented in the core helper services.

## API

### Constructor
- `public RetryHelperTests()`
  - Purpose: Initializes a new instance of the test class.

### Asynchronous Retry Tests
- `public async Task ExecuteWithRetryAsync_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()`
  - Purpose: Verifies `ExecuteWithRetryAsync` returns the expected result when the operation completes successfully on the first attempt.
- `public async Task ExecuteWithRetryAsync_WhenOperationFailsInitiallyThenSucceeds_ReturnsResult()`
  - Purpose: Verifies `ExecuteWithRetryAsync` correctly retries and ultimately returns the result when an operation fails initially but succeeds on a subsequent attempt.
- `public async Task ExecuteWithRetryAsync_WhenOperationExceedsMaxRetries_ThrowsException()`
  - Purpose: Ensures `ExecuteWithRetryAsync` throws an exception when the maximum number of configured retry attempts is exceeded without success.
- `public async Task ExecuteWithRetryAsync_WithInvalidMaxRetries_ThrowsArgumentException()`
  - Purpose: Verifies `ExecuteWithRetryAsync` throws an `ArgumentException` when provided with an invalid (e.g., negative) `maxRetries` value.
- `public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_RetriesOnlyForRetryableExceptions()`
  - Purpose: Verifies that `ExecuteWithRetryAsync` only performs retries when the exception thrown matches the condition defined in the `shouldRetry` predicate.
- `public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_SucceedsWhenRetryableExceptionResolved()`
  - Purpose: Verifies that `ExecuteWithRetryAsync` successfully recovers when a retryable exception is resolved on a subsequent attempt, as guided by the `shouldRetry` predicate.
- `public async Task ExecuteWithRetryAsync_LogsWarningOnRetryableFailure()`
  - Purpose: Verifies that `ExecuteWithRetryAsync` emits appropriate warning logs when a retryable failure occurs.
- `public async Task ExecuteWithRetryAsync_LogsErrorAfterMaxRetriesExceeded()`
  - Purpose: Verifies that `ExecuteWithRetryAsync` emits an error log when the operation fails permanently after exceeding the maximum retries.
- `public async Task ExecuteWithRetryAsync_ImplmentsExponentialBackoff_IncreaseDelayBetweenRetries()`
  - Purpose: Verifies that `ExecuteWithRetryAsync` correctly increases the delay between retry attempts in accordance with the exponential backoff strategy.

### Synchronous Retry Tests
- `public void ExecuteWithRetry_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()`
  - Purpose: Verifies `ExecuteWithRetry` returns the expected result when the operation completes successfully on the first attempt.
- `public void ExecuteWithRetry_WhenOperationFailsInitiallyThenSucceeds_ReturnsResult()`
  - Purpose: Verifies `ExecuteWithRetry` correctly retries and returns the result when an operation fails initially but succeeds on a subsequent attempt.
- `public void ExecuteWithRetry_WhenOperationExceedsMaxRetries_ThrowsException()`
  - Purpose: Ensures `ExecuteWithRetry` throws an exception when the maximum number of configured retry attempts is exceeded.
- `public void ExecuteWithRetry_WithInvalidMaxRetries_ThrowsArgumentException()`
  - Purpose: Verifies `ExecuteWithRetry` throws an `ArgumentException` when provided with an invalid `maxRetries` value.

### Circuit Breaker Tests
- `public async Task ExecuteWithCircuitBreakerAsync_WhenOperationSucceeds_ReturnsSuccessTrue()`
  - Purpose: Verifies `ExecuteWithCircuitBreakerAsync` returns a result indicating success when the underlying operation completes without error.
- `public async Task ExecuteWithCircuitBreakerAsync_WhenOperationFails_ReturnsSuccessFalse()`
  - Purpose: Verifies `ExecuteWithCircuitBreakerAsync` returns a result indicating failure when the underlying operation encounters an error.

## Usage

### Example 1: Asynchronous Retry
```csharp
[Fact]
public async Task ExecuteAsync_DataFetchExample()
{
    var retryHelper = new RetryHelper();
    // Execute an async operation with a maximum of 3 retries
    var result = await retryHelper.ExecuteWithRetryAsync(
        async () => await _notionClient.GetDatabaseAsync("db-id"),
        maxRetries: 3
    );
    Assert.NotNull(result);
}
```

### Example 2: Circuit Breaker
```csharp
[Fact]
public async Task CircuitBreaker_ServiceCallExample()
{
    var circuitBreaker = new CircuitBreaker();
    // Execute operation protected by a circuit breaker
    var response = await circuitBreaker.ExecuteWithCircuitBreakerAsync(
        async () => await _apiClient.PostDataAsync(payload)
    );
    Assert.True(response.Success);
}
```

## Notes

- **Thread Safety:** While the `RetryHelperTests` themselves are typically executed by a test runner in a manner that isolates test state (e.g., each test method is an independent execution), the `RetryHelper` implementation being tested must be designed to be thread-safe if it is to be utilized in high-concurrency environments within the main application.
- **Dependency Injection:** These tests assume that `RetryHelper` and any associated circuit breakers can be instantiated and configured either through direct instantiation or via injected dependencies, depending on the architecture of the `RetryHelper` class itself.
- **Exception Scenarios:** The tests explicitly cover edge cases such as invalid arguments and exhaustion of retry attempts, ensuring that the `RetryHelper` behaves predictably under stress and invalid configuration.
