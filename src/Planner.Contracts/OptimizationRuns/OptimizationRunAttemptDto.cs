namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunAttemptDto(
    string AttemptId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? WorkerId,
    int? DeliveryCount,
    OptimizationRunStatus Status,
    string? ErrorMessage);
