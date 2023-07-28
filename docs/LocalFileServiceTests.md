# LocalFileServiceTests

`LocalFileServiceTests` is a test suite designed to verify the functionality, error handling, and file system interactions of the `LocalFileService` component. It ensures that task data is accurately serialized, stored, retrieved, and managed within the local file system, validating behavior across valid, invalid, and edge-case scenarios.

## API

### Constructor and Lifecycle
*   **`Dispose`**: Performs necessary cleanup of temporary files and resources utilized during the execution of test cases to maintain a clean environment.
*   **`Constructor_WithNullBasePath_ThrowsArgumentNullException`**: Verifies that the service constructor throws an `ArgumentNullException` when the provided base path is `null`.
*   **`Constructor_WithEmptyBasePath_ThrowsArgumentNullException`**: Verifies that the service constructor throws an `ArgumentNullException` when the provided base path is an empty string.

### SaveTaskAsync Tests
*   **`SaveTaskAsync_WithValidTask_CreatesFileWithTaskContent`**: Validates that a valid task is successfully serialized and written to a file with the expected content.
*   **`SaveTaskAsync_WithMultipleTasks_CreatesSeparateFiles`**: Confirms that saving multiple distinct tasks results in the creation of separate, appropriately named files on the disk.
*   **`SaveTaskAsync_WithInvalidTask_ThrowsValidationException`**: Ensures that the method throws a `ValidationException` when attempting to save an task that fails validation rules.
*   **`SaveTaskAsync_WithSpecialCharactersInTitle_SanitizesFileName`**: Verifies that task titles containing characters illegal for file system paths are correctly sanitized to generate valid file names.
*   **`SaveTaskAsync_WithSameTitle_OverwritesExistingFile`**: Confirms that saving a task with a title identical to an existing file correctly overwrites the existing file content.
*   **`SaveTaskAsync_UpdatesLocalFilePathProperty`**: Verifies that the internal file path property of the task model is updated correctly upon a successful save operation.
*   **`SaveTaskAsync_WhenDirectoryDoesNotExist_CreatesDirectory`**: Validates that the service automatically creates the target storage directory if it does not already exist when attempting to save a task.

### LoadTaskAsync Tests
*   **`LoadTaskAsync_WithValidFilePath_ReturnsTask`**: Validates that a task is successfully deserialized and returned when loading from a valid file path.
*   **`LoadTaskAsync_WithNonExistentFile_ReturnsNull`**: Confirms that the method returns `null` when attempting to load a task from a file path that does not exist.
*   **`LoadTaskAsync_WithNullFilePath_ReturnsNull`**: Ensures the method returns `null` when the provided file path is `null`.
*   **`LoadTaskAsync_WithEmptyFilePath_ReturnsNull`**: Ensures the method returns `null` when the provided file path is an empty string.
*   **`LoadTaskAsync_WithInvalidMarkdownFormat_ThrowsLocalFileException`**: Verifies that the method throws a `LocalFileException` when encountering malformed markdown content during the deserialization process.

### LoadAllTasksAsync Tests
*   **`LoadAllTasksAsync_WithMultipleTasks_ReturnsAllTasks`**: Validates that loading from a directory containing multiple task files returns the expected collection containing all stored tasks.
*   **`LoadAllTasksAsync_WithEmptyDirectory_ReturnsEmptyList`**: Confirms that loading tasks from an empty directory returns an empty list, rather than throwing an exception.
*   **`LoadAllTasksAsync_WithNonExistentDirectory_ReturnsEmptyList`**: Confirms that loading tasks from a directory path that does not exist returns an empty list.

## Usage

### Saving a Task
```csharp
[Fact]
public async Task Save_Example()
{
    var service = new LocalFileService("./test_tasks");
    var task = new TaskItem { Title = "Project Setup", Content = "Initialize repository." };

    await service.SaveTaskAsync(task);
    
    // File "./test_tasks/Project_Setup.md" is created
    Assert.True(File.Exists(task.LocalFilePath));
}
```

### Loading All Tasks
```csharp
[Fact]
public async Task LoadAll_Example()
{
    var service = new LocalFileService("./test_tasks");
    
    var tasks = await service.LoadAllTasksAsync();
    
    Assert.NotNull(tasks);
    Assert.IsAssignableFrom<IEnumerable<TaskItem>>(tasks);
}
```

## Notes

*   **File System Dependencies**: These tests interact directly with the local file system. It is strongly recommended to use a unique, dedicated temporary directory for each test run to prevent cross-test interference.
*   **Thread Safety**: `LocalFileService` performs I/O operations. Because these tests manipulate the same directory structure, they are not inherently thread-safe if executed in parallel. Test runners should be configured to run these tests sequentially or use isolated test directories per test case.
*   **Edge Cases**: The suite covers common file system issues such as missing directories, invalid characters, and file overwriting logic. It does not explicitly test low-level OS constraints, such as file system permissions, disk space exhaustion, or file locking by external processes.
