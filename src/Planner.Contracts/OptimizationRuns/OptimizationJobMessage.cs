namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationJobMessage(Guid TenantId, Guid OptimizationRunId);
