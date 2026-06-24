namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunTimelineEventDto(
    Guid EventId,
    OptimizationRunStatus Status,
    DateTime AtUtc,
    string Message);
