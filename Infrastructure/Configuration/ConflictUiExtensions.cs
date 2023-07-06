#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using Microsoft.Extensions.DependencyInjection;
using NotionTaskSync.Services;

/// <summary>
/// Extension methods for registering conflict resolution UI services into the DI container.
/// Call <see cref="AddConflictResolutionUi"/> alongside
/// <see cref="DependencyInjection.AddApplicationServices"/> during startup to make
/// <see cref="ConflictDiffService"/> available for injection wherever diff previews are needed.
/// </summary>
public static class ConflictUiExtensions
{
    /// <summary>
    /// Registers <see cref="ConflictDiffService"/> as a singleton so that the LCS diff
    /// engine and its rendering pipeline are shared across the application lifetime.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance to support chaining.</returns>
    public static IServiceCollection AddConflictResolutionUi(
        this IServiceCollection services)
    {
        services.AddSingleton<ConflictDiffService>();
        return services;
    }
}
