# ConfigRepository

Centralizes access to synchronization configuration files, providing methods to persist, retrieve, delete, and transfer `SyncConfig` instances. It abstracts file I/O operations and ensures consistent handling of configuration data across the `notion-task-sync` application.

## API

### `public ConfigRepository`

Initializes a new instance of the `ConfigRepository` class. The repository operates on the default configuration directory determined by the application's environment or runtime settings.

### `public async Task<bool> SaveConfigAsync`

Persists the provided `SyncConfig` instance to disk.

- **Parameters**
  - `config` – The `SyncConfig` instance to save.
- **Return value**
  - `true` if the configuration was successfully saved; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `config` is `null`.
  - Throws `IOException` if the file system operation fails.

### `public async Task<SyncConfig?> GetConfigAsync`

Retrieves the configuration associated with the specified identifier.

- **Parameters**
  - `id` – The unique identifier of the configuration to retrieve.
- **Return value**
  - The `SyncConfig` instance if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentException` if `id` is empty or whitespace.

### `public async Task<List<SyncConfig>> GetAllConfigsAsync`

Enumerates all available configurations stored in the repository.

- **Return value**
  - A list of `SyncConfig` instances, possibly empty if no configurations exist.
- **Exceptions**
  - Throws `IOException` if the directory cannot be read.

### `public bool DeleteConfig`

Removes the configuration associated with the specified identifier.

- **Parameters**
  - `id` – The unique identifier of the configuration to delete.
- **Return value**
  - `true` if the configuration existed and was deleted; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentException` if `id` is empty or whitespace.

### `public async Task<bool> ExportConfigAsync`

Serializes and writes the specified configuration to an external file path.

- **Parameters**
  - `config` – The `SyncConfig` instance to export.
  - `filePath` – The destination file path where the configuration will be saved.
- **Return value**
  - `true` if the export succeeded; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `config` or `filePath` is `null`.
  - Throws `ArgumentException` if `filePath` is empty or whitespace.
  - Throws `IOException` if the file cannot be written.

### `public async Task<SyncConfig?> ImportConfigAsync`

Reads and deserializes a configuration from an external file path.

- **Parameters**
  - `filePath` – The source file path from which to import the configuration.
- **Return value**
  - The deserialized `SyncConfig` instance if successful; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is `null`.
  - Throws `ArgumentException` if `filePath` is empty or whitespace.
  - Throws `IOException` if the file cannot be read or parsed.

### `public bool ConfigExists`

Checks whether a configuration with the specified identifier exists.

- **Parameters**
  - `id` – The unique identifier of the configuration to check.
- **Return value**
  - `true` if the configuration exists; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentException` if `id` is empty or whitespace.

## Usage
