namespace Planner.Contracts.OptimizationRuns;

public enum OptimizationRunStatus {
    Created,
    Queued,
    Running,
    Succeeded,
    Failed,
    DeadLettered,
    Cancelled
}
