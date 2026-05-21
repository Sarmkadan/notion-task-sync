#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Repositories;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NotionTaskSync.Domain.Models;
using NotionTaskSync.Formatters;
using NotionTaskSync.Utils;
using Microsoft.Extensions.Logging;

/// <summary>
/// Repository for persisting and retrieving sync configurations.
/// Stores configurations as JSON files for easy portability and version control.
/// Enables users to define multiple sync profiles for different Notion databases.
/// </summary>
public class ConfigRepository
{
    private readonly string _configDirectory;
    private readonly FileSystemHelper _fileSystemHelper;
    private readonly JsonFormatter _jsonFormatter;
    private readonly ILogger<ConfigRepository> _logger;

    public ConfigRepository(
        string configDirectory,
        FileSystemHelper fileSystemHelper,
        JsonFormatter jsonFormatter,
        ILogger<ConfigRepository> logger)
    {
        _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
        _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        _jsonFormatter = jsonFormatter ?? throw new ArgumentNullException(nameof(jsonFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure config directory exists
        _fileSystemHelper.EnsureDirectoryExists(_configDirectory);
    }

    /// <summary>
    /// Saves a configuration to a JSON file.
    /// Overwrites existing configuration with the same name.
    /// </summary>
    public async Task<bool> SaveConfigAsync(SyncConfig config)
    {
        try
        {
            var fileName = GetConfigFileName(config.Name);
            var filePath = Path.Combine(_configDirectory, fileName);

            var json = _jsonFormatter.Format(config);
            var success = await _fileSystemHelper.WriteFileAsync(filePath, json).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation("Configuration saved: {ConfigName} ({FilePath})",
                    config.Name, filePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration: {ConfigName}", config.Name);
            return false;
        }
    }

    /// <summary>
    /// Loads a configuration by name.
    /// Returns null if configuration doesn't exist.
    /// </summary>
    public async Task<SyncConfig?> GetConfigAsync(string configName)
    {
        try
        {
            var fileName = GetConfigFileName(configName);
            var filePath = Path.Combine(_configDirectory, fileName);

            var json = await _fileSystemHelper.ReadFileAsync(filePath).ConfigureAwait(false);
            if (json is null)
                return null;

            var config = _jsonFormatter.Deserialize<SyncConfig>(json);
            _logger.LogInformation("Configuration loaded: {ConfigName}", configName);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration: {ConfigName}", configName);
            return null;
        }
    }

    /// <summary>
    /// Gets all saved configurations.
    /// </summary>
    public async Task<List<SyncConfig>> GetAllConfigsAsync()
    {
        var configs = new List<SyncConfig>();

        try
        {
            var directory = new DirectoryInfo(_configDirectory);
            if (!directory.Exists)
                return configs;

            foreach (var file in directory.GetFiles("*.json"))
            {
                var json = await _fileSystemHelper.ReadFileAsync(file.FullName).ConfigureAwait(false);
                if (json is not null)
                {
                    var config = _jsonFormatter.Deserialize<SyncConfig>(json);
                    if (config is not null)
                    {
                        configs.Add(config);
                    }
                }
            }

            _logger.LogInformation("Loaded {ConfigCount} configurations", configs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load all configurations");
        }

        return configs;
    }

    /// <summary>
    /// Deletes a configuration by name.
    /// </summary>
    public bool DeleteConfig(string configName)
    {
        try
        {
            var fileName = GetConfigFileName(configName);
            var filePath = Path.Combine(_configDirectory, fileName);

            var success = _fileSystemHelper.DeleteFile(filePath);
            if (success)
            {
                _logger.LogInformation("Configuration deleted: {ConfigName}", configName);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete configuration: {ConfigName}", configName);
            return false;
        }
    }

    /// <summary>
    /// Exports a configuration to a specific file path.
    /// Useful for sharing or backing up configurations.
    /// </summary>
    public async Task<bool> ExportConfigAsync(SyncConfig config, string exportPath)
    {
        try
        {
            var json = _jsonFormatter.Format(config);
            var success = await _fileSystemHelper.WriteFileAsync(exportPath, json).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation("Configuration exported: {ConfigName} to {ExportPath}",
                    config.Name, exportPath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration to {ExportPath}", exportPath);
            return false;
        }
    }

    /// <summary>
    /// Imports a configuration from a file.
    /// </summary>
    public async Task<SyncConfig?> ImportConfigAsync(string importPath)
    {
        try
        {
            var json = await _fileSystemHelper.ReadFileAsync(importPath).ConfigureAwait(false);
            if (json is null)
            {
                _logger.LogWarning("Import file not found: {ImportPath}", importPath);
                return null;
            }

            var config = _jsonFormatter.Deserialize<SyncConfig>(json);
            if (config is not null)
            {
                // Save the imported configuration
                await SaveConfigAsync(config).ConfigureAwait(false);
                _logger.LogInformation("Configuration imported from {ImportPath}", importPath);
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import configuration from {ImportPath}", importPath);
            return null;
        }
    }

    /// <summary>
    /// Checks if a configuration exists.
    /// </summary>
    public bool ConfigExists(string configName)
    {
        var fileName = GetConfigFileName(configName);
        var filePath = Path.Combine(_configDirectory, fileName);
        return _fileSystemHelper.IsFile(filePath);
    }

    /// <summary>
    /// Generates a safe filename from a configuration name.
    /// Sanitizes the name to remove invalid filesystem characters.
    /// </summary>
    private string GetConfigFileName(string configName)
    {
        var sanitized = configName.SanitizeForFilename();
        return $"{sanitized}.json";
    }
}
