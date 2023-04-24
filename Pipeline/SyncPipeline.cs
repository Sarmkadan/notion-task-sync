#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Pipeline pattern implementation for sync operations.
/// Chains together multiple operations with middleware-style processing.
/// Provides extensible architecture for adding custom sync steps without modifying core logic.
/// </summary>
public class SyncPipeline
{
    private readonly List<ISyncStep> _steps = new();
    private readonly ILogger<SyncPipeline> _logger;

    public SyncPipeline(ILogger<SyncPipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a step to the pipeline.
    /// Steps execute in the order they are added.
    /// </summary>
    public void AddStep(ISyncStep step)
    {
        if (step is null)
            throw new ArgumentNullException(nameof(step));

        _steps.Add(step);
        _logger.LogDebug("Added step to pipeline: {StepName}", step.Name);
    }

    /// <summary>
    /// Executes the entire pipeline with provided context.
    /// Returns result containing success status and any errors.
    /// </summary>
    public async Task<PipelineResult> ExecuteAsync(PipelineContext context)
    {
        var result = new PipelineResult();

        _logger.LogInformation("Starting sync pipeline with {StepCount} steps", _steps.Count);

        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];

            try
            {
                _logger.LogDebug("Executing step {StepNumber}/{TotalSteps}: {StepName}",
                    i + 1, _steps.Count, step.Name);

                // Execute the step
                var stepResult = await step.ExecuteAsync(context);

                // Track step result
                result.StepResults.Add(new StepResult
                {
                    StepName = step.Name,
                    Success = stepResult,
                    ExecutedAt = DateTime.UtcNow
                });

                // If step failed and it's critical, stop the pipeline
                if (!stepResult && step.IsCritical)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Critical step '{step.Name}' failed. Pipeline aborted.";
                    _logger.LogError(result.ErrorMessage);
                    break;
                }

                // If step failed but not critical, log warning and continue
                if (!stepResult && !step.IsCritical)
                {
                    _logger.LogWarning("Non-critical step '{StepName}' failed. Continuing pipeline.",
                        step.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in step '{StepName}'", step.Name);

                result.StepResults.Add(new StepResult
                {
                    StepName = step.Name,
                    Success = false,
                    ExecutedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });

                if (step.IsCritical)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Exception in critical step '{step.Name}': {ex.Message}";
                    break;
                }
            }
        }

        // Mark as successful only if no critical failures
        if (result.ErrorMessage is null)
            result.Success = true;

        _logger.LogInformation("Sync pipeline completed. Success: {Success}, Steps executed: {StepCount}",
            result.Success, result.StepResults.Count);

        return result;
    }

    /// <summary>
    /// Gets the list of steps in the pipeline.
    /// </summary>
    public IReadOnlyList<ISyncStep> GetSteps() => _steps.AsReadOnly();

    /// <summary>
    /// Clears all steps from the pipeline.
    /// </summary>
    public void Clear()
    {
        _steps.Clear();
        _logger.LogDebug("Pipeline cleared");
    }
}

/// <summary>
/// Interface for pipeline steps.
/// Implementations handle specific parts of the sync operation.
/// </summary>
public interface ISyncStep
{
    /// <summary>
    /// Name of this step for logging and identification.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this step is critical (stops pipeline on failure).
    /// </summary>
    bool IsCritical { get; }

    /// <summary>
    /// Executes the step with provided context.
    /// Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> ExecuteAsync(PipelineContext context);
}

/// <summary>
/// Context passed through the pipeline.
/// Accumulates data as it flows through steps.
/// </summary>
public class PipelineContext
{
    /// <summary>
    /// Shared data dictionary for passing information between steps.
    /// </summary>
    public Dictionary<string, object?> Data { get; } = new();

    /// <summary>
    /// Diagnostic messages from steps.
    /// </summary>
    public List<string> Messages { get; } = new();

    /// <summary>
    /// Gets or sets a value in the context data.
    /// </summary>
    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value))
            return (T?)value;
        return default;
    }

    /// <summary>
    /// Sets a value in the context data.
    /// </summary>
    public void SetData<T>(string key, T value)
    {
        Data[key] = value;
    }

    /// <summary>
    /// Adds a diagnostic message.
    /// </summary>
    public void AddMessage(string message)
    {
        Messages.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
    }
}

/// <summary>
/// Result of executing the pipeline.
/// </summary>
public class PipelineResult
{
    /// <summary>
    /// Whether the entire pipeline succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if pipeline failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Results from individual steps.
    /// </summary>
    public List<StepResult> StepResults { get; } = new();
}

/// <summary>
/// Result of a single step execution.
/// </summary>
public class StepResult
{
    /// <summary>
    /// Name of the step.
    /// </summary>
    public required string StepName { get; set; }

    /// <summary>
    /// Whether the step succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// When the step executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Error message if step failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
