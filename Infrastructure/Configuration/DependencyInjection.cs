#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NotionTaskSync.Services;
using NotionTaskSync.Data.Repositories;
using NotionTaskSync.Commands;
using System;

/// <summary>
/// Configures dependency injection for the application.
/// Registers services, repositories, and configuration objects.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all application services and repositories into the DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration objects
        services.Configure<NotionApiSettings>(
            configuration.GetSection("NotionApi"));

        services.Configure<AppSettings>(
            configuration.GetSection("AppSettings"));

        // Register repositories
        services.AddSingleton<ITaskRepository, TaskRepository>();
        services.AddSingleton<IChangeLogRepository, ChangeLogRepository>();

            // Register LocalFileService with base path from configuration
            services.AddSingleton<LocalFileService>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var basePath = appSettings.LocalTasksDirectory ?? "./tasks";
                return new LocalFileService(basePath);
            });

        // Register services
        services.AddHttpClients();
        services.AddSingleton<NotionApiService>(sp =>
        {
            var notionApiConfig = configuration.GetSection("NotionApi");
            var apiKey = notionApiConfig["ApiKey"];

            // Use the factory-managed client so handler lifetime/pooling is
            // handled by HttpClientFactory instead of a raw new HttpClient().
            var httpClient = sp
                .GetRequiredService<System.Net.Http.IHttpClientFactory>()
                .CreateClient("NotionApi");

            // Get included statuses from configuration
            var includedStatusesConfig = notionApiConfig.GetValue<List<string>>("IncludedStatuses");
            var includedStatuses = includedStatusesConfig ?? new List<string>();

            return new NotionApiService(apiKey, httpClient, includedStatuses);
        });

        services.AddSingleton<ISyncCheckpointStore>(new SyncCheckpointStore());
        services.AddSingleton<ChangeDetectionService>();
        services.AddSingleton<ConflictResolutionService>();
        services.AddSingleton<ConflictDiffService>();
        services.AddSingleton<SyncService>();


            // Register backup service with configuration
            services.AddSingleton<BackupService>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var fileService = sp.GetRequiredService<LocalFileService>();

                return new BackupService(
                    backupDirectory: appSettings.BackupDirectory ?? "./backups",
                    maxBackupFiles: appSettings.MaxBackupFiles,
                    fileService: fileService
                );
            });
        // Calendar sync
        services.AddSingleton<CalendarSyncService>();

        // Bulk operations
        services.AddSingleton<BulkOperationService>();

        // CLI commands
        services.AddSingleton<CalendarCommand>();
        services.AddSingleton<BulkCommand>();
        services.AddSingleton<ConflictCommand>();

        return services;
    }

    /// <summary>
    /// Validates that all required configuration is present and valid.
    /// </summary>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var notionApiSection = configuration.GetSection("NotionApi");

        if (!notionApiSection.Exists())
        {
            throw new ConfigurationException(
                "NotionApi configuration section is missing. " +
                "Please add NotionApi section to appsettings.json");
        }

        var apiKey = notionApiSection["ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException(
                "NotionApi:ApiKey is missing. " +
                "Please provide a valid Notion API key in appsettings.json");
        }

        var appSettingsSection = configuration.GetSection("AppSettings");

        if (!appSettingsSection.Exists())
        {
            throw new ConfigurationException(
                "AppSettings configuration section is missing. " +
                "Please add AppSettings section to appsettings.json");
        }
    }

    /// <summary>
    /// Registers HTTP clients for API communication.
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddHttpClient("NotionApi", client =>
        {
            client.BaseAddress = new Uri("https://api.notion.com/v1");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        });

        return services;
    }
}

/// <summary>
/// Configuration exception for DI and setup errors.
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}
