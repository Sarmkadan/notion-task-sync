#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Infrastructure.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NotionTaskSync.Domain.Models;

/// <summary>
/// Validates <see cref="NotionSyncOptions"/> at startup. Every failing property is reported,
/// and each message names the property (as <c>NotionSync:&lt;Property&gt;</c>) and the reason.
/// </summary>
public sealed class NotionSyncOptionsValidator : IValidateOptions<NotionSyncOptions>
{
    private static readonly TimeSpan MinimumSyncInterval = TimeSpan.FromMinutes(1);

    private static readonly Regex DashedUuidPattern = new(
        @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PlainUuidPattern = new(
        @"^[0-9a-fA-F]{32}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates the sync options and returns every failure found.
    /// </summary>
    /// <param name="name">The options instance name (unused; validation applies to all names).</param>
    /// <param name="options">The options to validate.</param>
    /// <returns><see cref="ValidateOptionsResult.Success"/> when valid; otherwise a failed result listing each problem.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public ValidateOptionsResult Validate(string? name, NotionSyncOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            failures.Add(Message(nameof(NotionSyncOptions.ApiToken), "is required but is missing or blank."));
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseId))
        {
            failures.Add(Message(nameof(NotionSyncOptions.DatabaseId), "is required but is missing or blank."));
        }
        else if (!IsValidNotionUuid(options.DatabaseId))
        {
            failures.Add(Message(nameof(NotionSyncOptions.DatabaseId),
                $"must be a Notion UUID (32 hex digits, optionally in 8-4-4-4-12 dashed form); was '{options.DatabaseId}'."));
        }

        if (options.SyncInterval < MinimumSyncInterval)
        {
            failures.Add(Message(nameof(NotionSyncOptions.SyncInterval),
                $"must be at least 1 minute; was {options.SyncInterval}."));
        }

        if (!IsValidConflictStrategy(options.ConflictStrategy))
        {
            failures.Add(Message(nameof(NotionSyncOptions.ConflictStrategy),
                $"'{options.ConflictStrategy}' is not a known strategy. Valid values: " +
                $"{string.Join(", ", Enum.GetNames<ConflictResolutionStrategy>())}."));
        }

        if (string.IsNullOrWhiteSpace(options.LocalStorePath))
        {
            failures.Add(Message(nameof(NotionSyncOptions.LocalStorePath), "is required but is missing or blank."));
        }
        else if (!Path.IsPathFullyQualified(options.LocalStorePath))
        {
            failures.Add(Message(nameof(NotionSyncOptions.LocalStorePath),
                $"must be an absolute path; was '{options.LocalStorePath}'."));
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static string Message(string property, string reason) =>
        $"{NotionSyncOptions.SectionName}:{property} {reason}";

    private static bool IsValidNotionUuid(string value) =>
        DashedUuidPattern.IsMatch(value) || PlainUuidPattern.IsMatch(value);

    private static bool IsValidConflictStrategy(string? value) =>
        value is not null &&
        Array.Exists(Enum.GetNames<ConflictResolutionStrategy>(),
            strategy => string.Equals(strategy, value, StringComparison.OrdinalIgnoreCase));
}
