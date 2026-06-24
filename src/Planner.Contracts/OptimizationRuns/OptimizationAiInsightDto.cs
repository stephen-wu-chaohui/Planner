namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationAiInsightDto(
    string Status,
    string? AnalysisMarkdown,
    DateTime UpdatedAtUtc,
    string? ErrorMessage);
