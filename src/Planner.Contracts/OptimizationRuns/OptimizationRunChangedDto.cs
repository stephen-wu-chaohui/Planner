namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunChangedDto(
    Guid TenantId,
    Guid OptimizationRunId,
    long Version,
    OptimizationRunStatus Status,
    DateTime UpdatedAtUtc,
    OptimizationRunSummaryDto Summary,
    bool HasResult,
    bool HasAiInsight,
    string? ErrorMessage);
