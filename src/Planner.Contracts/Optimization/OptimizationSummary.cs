namespace Planner.Contracts.Optimization;

public record OptimizationSummary(
    Guid TenantId,
    Guid OptimizationRunId,
    int JobCount,
    int VehicleCount,
    DateTime RequestedAt,
    int SearchTimeLimitSeconds
);
