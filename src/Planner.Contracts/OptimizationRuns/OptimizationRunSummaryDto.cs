namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunSummaryDto(
    int JobCount,
    int VehicleCount,
    DateTime RequestedAtUtc,
    int SearchTimeLimitSeconds,
    string? RequestedBy);
