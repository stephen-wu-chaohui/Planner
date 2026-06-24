namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunStatusDto(
    Guid TenantId,
    Guid OptimizationRunId,
    long Version,
    OptimizationRunStatus Status,
    DateTime RequestedAtUtc,
    DateTime UpdatedAtUtc,
    OptimizationRunSummaryDto Summary,
    bool HasResult,
    bool HasAiInsight,
    string? ErrorMessage,
    IReadOnlyList<OptimizationRunTimelineEventDto> Timeline);
