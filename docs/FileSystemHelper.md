# FileSystemHelper

A utility class that provides common file system operations such as directory and file manipulation, path normalization, and metadata queries. Designed to simplify filesystem interactions with consistent error handling and platform-aware path operations.

## API

### `FileSystemHelper`
Initializes a new instance of the `FileSystemHelper` class. This constructor does not require any parameters and prepares the helper for filesystem operations.

### `EnsureDirectoryExists`
Ensures that the specified directory exists. If the directory does not exist, it will be created, including any necessary parent directories.

- **Parameters**
  - `path` (string): The path to the directory to ensure exists.
- **Return value**
  - `bool`: `true` if the directory already existed or was successfully created; `false` if creation failed.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks permissions.
  - Throws `PathTooLongException` if the path exceeds system-defined maximum length.
  - Throws `DirectoryNotFoundException` if a parent directory is invalid and cannot be created.

### `ReadFileAsync`
Asynchronously reads the entire contents of a file as a string.

- **Parameters**
  - `path` (string): The path to the file to read.
- **Return value**
  - `Task<string?>`: A task that represents the asynchronous operation. The result is the file content as a string, or `null` if the file does not exist or cannot be read.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks read permissions.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file is locked or an I/O error occurs.

### `WriteFileAsync`
Asynchronously writes the specified string content to a file. If the file exists, it will be overwritten.

- **Parameters**
  - `path` (string): The path to the file to write.
  - `content` (string): The content to write to the file.
- **Return value**
  - `Task<bool>`: A task that represents the asynchronous operation. The result is `true` if the file was successfully written; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` or `content` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks write permissions.
  - Throws `DirectoryNotFoundException` if the parent directory does not exist and cannot be created.
  - Throws `IOException` if the file is locked or an I/O error occurs.

### `AppendFileAsync`
Asynchronously appends the specified string content to a file. If the file does not exist, it will be created.

- **Parameters**
  - `path` (string): The path to the file to append to.
  - `content` (string): The content to append to the file.
- **Return value**
  - `Task<bool>`: A task that represents the asynchronous operation. The result is `true` if the content was successfully appended; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` or `content` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks write permissions.
  - Throws `DirectoryNotFoundException` if the parent directory does not exist and cannot be created.
  - Throws `IOException` if the file is locked or an I/O error occurs.

### `DeleteFile`
Deletes the specified file.

- **Parameters**
  - `path` (string): The path to the file to delete.
- **Return value**
  - `bool`: `true` if the file was successfully deleted or did not exist; `false` if deletion failed.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks delete permissions.
  - Throws `IOException` if the file is locked or an I/O error occurs.

### `DeleteDirectory`
Deletes the specified directory and, if specified, its contents recursively.

- **Parameters**
  - `path` (string): The path to the directory to delete.
  - `recursive` (bool): If `true`, deletes the directory and all its contents; otherwise, deletes only if the directory is empty.
- **Return value**
  - `bool`: `true` if the directory was successfully deleted or did not exist; `false` if deletion failed.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks delete permissions.
  - Throws `DirectoryNotFoundException` if the directory does not exist.
  - Throws `IOException` if the directory is in use or an I/O error occurs.

### `CopyFile`
Copies a file from one location to another. If the destination file exists, it will be overwritten.

- **Parameters**
  - `sourcePath` (string): The path to the source file.
  - `destinationPath` (string): The path to the destination file.
- **Return value**
  - `bool`: `true` if the file was successfully copied; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `sourcePath` or `destinationPath` is `null`.
  - Throws `ArgumentException` if either path is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks read or write permissions.
  - Throws `FileNotFoundException` if the source file does not exist.
  - Throws `DirectoryNotFoundException` if the destination directory does not exist and cannot be created.
  - Throws `IOException` if the source file is locked or an I/O error occurs.

### `GetFileSize`
Gets the size in bytes of the specified file.

- **Parameters**
  - `path` (string): The path to the file.
- **Return value**
  - `long`: The size of the file in bytes, or `-1` if the file does not exist or cannot be accessed.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks read permissions.

### `IsDirectory`
Determines whether the specified path refers to an existing directory.

- **Parameters**
  - `path` (string): The path to check.
- **Return value**
  - `bool`: `true` if the path exists and is a directory; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.

### `IsFile`
Determines whether the specified path refers to an existing file.

- **Parameters**
  - `path` (string): The path to check.
- **Return value**
  - `bool`: `true` if the path exists and is a file; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.

### `NormalizePath`
Normalizes a file system path by converting separators to the platform-specific format and resolving relative segments (e.g., `.` and `..`).

- **Parameters**
  - `path` (string): The path to normalize.
- **Return value**
  - `string`: The normalized path.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.

### `GetLastModifiedTime`
Gets the last modified time of the specified file or directory.

- **Parameters**
  - `path` (string): The path to the file or directory.
- **Return value**
  - `DateTime`: The last modified time, or `DateTime.MinValue` if the path does not exist or cannot be accessed.
- **Exceptions**
  - Throws `ArgumentNullException` if `path` is `null`.
  - Throws `ArgumentException` if `path` is empty or whitespace.
  - Throws `UnauthorizedAccessException` if the caller lacks read permissions.

## Usage
