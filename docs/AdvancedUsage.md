# AdvancedUsage

The `AdvancedUsage` class provides a set of static methods for demonstrating advanced synchronization patterns and configuration options in the `notion-task-sync` project. These methods illustrate how to extend basic task synchronization with conditional logic, option handling, and custom configuration workflows.

## API

### `public static async Task RunAdvancedConfigurationExample()`

Demonstrates how to apply advanced configuration settings to the synchronization process. This method initializes a synchronization session with custom settings that control behavior such as field mapping, conflict resolution, and rate limiting.

- **Parameters**: None
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `InvalidOperationException` if the synchronization context is not properly initialized.
- **Throws**: `NotionApiException` if the Notion API returns an error during configuration.

### `public static async Task RunWithOptionsPattern()`

Shows how to use the options pattern to configure synchronization behavior programmatically. This method accepts a set of options that define filtering rules, field mappings, and synchronization modes, allowing for flexible and reusable configuration.

- **Parameters**: None
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if required options are not provided.
- **Throws**: `NotionApiException` if the Notion API rejects the configuration.

### `public static async Task RunConditionalSyncExample()`

Illustrates how to perform conditional synchronization based on task properties or external state. This method evaluates tasks against user-defined conditions (e.g., due date, priority, or custom tags) and synchronizes only those that meet the criteria.

- **Parameters**: None
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `InvalidOperationException` if the condition evaluator is not initialized.
- **Throws**: `NotionApiException` if the Notion API request fails during evaluation.

### `public static async Task Main()`

Entry point for demonstrating advanced usage scenarios. This method orchestrates the execution of the other advanced examples in sequence, providing a cohesive demonstration of the project's extended capabilities.

- **Parameters**: None
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `InvalidOperationException` if any of the example methods fail or if the synchronization context is invalid.
- **Throws**: `AggregateException` if multiple asynchronous operations fail.

## Usage

### Example 1: Running advanced configuration with custom settings
