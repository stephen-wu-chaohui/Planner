namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationInsightChangedDto(
    Guid TenantId,
    Guid OptimizationRunId,
    long Version,
    DateTime UpdatedAtUtc,
    bool HasAiInsight,
    string? InsightStatus);
