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
/// <remarks>
/// This static class provides extension methods for configuring application services, HTTP clients,
/// and monitoring capabilities through dependency injection. All methods include proper null checking
/// and follow .NET dependency injection best practices.
/// </remarks>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers additional application services and configuration options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing application settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddAdditionalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure additional settings from configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Register additional services that extend existing functionality
        services.AddSingleton<ISyncCheckpointStore>(new SyncCheckpointStore());
        services.AddSingleton<ChangeDetectionService>();
        services.AddSingleton<ConflictResolutionService>();
        services.AddSingleton<ConflictDiffService>();

        return services;
    }

    /// <summary>
    /// Registers additional HTTP clients with custom configurations for API communication.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add HTTP clients to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddCustomHttpClients(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register additional HTTP clients with custom configurations
        services.AddHttpClient("SyncApi", client =>
        {
            client.BaseAddress = new Uri("https://api.sarmkadan.com/v1");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseProxy = true
        });

        services.AddHttpClient("WebhookService", client =>
        {
            client.BaseAddress = new Uri("https://webhooks.sarmkadan.com");
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = true
        });

        return services;
    }

    /// <summary>
    /// Registers monitoring and diagnostics services for tracking sync operations and system health.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddMonitoringServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register monitoring services
        services.AddSingleton<CalendarSyncService>();
        services.AddSingleton<BulkOperationService>();
        services.AddSingleton<SyncService>();

        return services;
    }

    /// <summary>
    /// Registers additional configuration options and services with validation.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing application settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddExtendedConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure additional settings from configuration with validation
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        return services;
    }
}