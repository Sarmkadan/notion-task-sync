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
using System;

/// <summary>
/// Extension methods for DependencyInjection to provide additional DI configuration options.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers additional application services and configuration options.
    /// </summary>
    public static IServiceCollection AddAdditionalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure additional settings from configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Register additional services that extend existing functionality
        services.AddSingleton<ChangeDetectionService>();
        services.AddSingleton<ConflictResolutionService>();
        services.AddSingleton<ConflictDiffService>();

        return services;
    }

    /// <summary>
    /// Registers additional HTTP clients with custom configurations.
    /// </summary>
    public static IServiceCollection AddCustomHttpClients(
        this IServiceCollection services)
    {
        // Register additional HTTP clients with custom configurations
        services.AddHttpClient("SyncApi", client =>
        {
            client.BaseAddress = new Uri("https://api.sarmkadan.com/v1");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("WebhookService", client =>
        {
            client.BaseAddress = new Uri("https://webhooks.sarmkadan.com");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    /// <summary>
    /// Registers monitoring and diagnostics services.
    /// </summary>
    public static IServiceCollection AddMonitoringServices(
        this IServiceCollection services)
    {
        // Register monitoring services
        services.AddSingleton<CalendarSyncService>();
        services.AddSingleton<BulkOperationService>();
        services.AddSingleton<SyncService>();

        return services;
    }

    /// <summary>
    /// Registers additional configuration options and services.
    /// </summary>
    public static IServiceCollection AddExtendedConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure additional settings from configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        return services;
    }
}