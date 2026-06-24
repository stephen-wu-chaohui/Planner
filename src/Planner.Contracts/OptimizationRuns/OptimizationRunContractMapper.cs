namespace Planner.Contracts.OptimizationRuns;

public static class OptimizationRunContractMapper {
    public static OptimizationRunStatusDto ToStatusDto(this OptimizationRunDocument run) =>
        new(
            run.TenantId,
            run.OptimizationRunId,
            run.Version,
            run.Status,
            run.RequestedAtUtc,
            run.UpdatedAtUtc,
            run.Summary,
            run.SolverResult is not null,
            run.AiInsight is not null,
            run.ErrorMessage,
            run.Timeline);

    public static OptimizationRunChangedDto ToRunChangedDto(this OptimizationRunDocument run) =>
        new(
            run.TenantId,
            run.OptimizationRunId,
            run.Version,
            run.Status,
            run.UpdatedAtUtc,
            run.Summary,
            run.SolverResult is not null,
            run.AiInsight is not null,
            run.ErrorMessage);

    public static OptimizationInsightChangedDto ToInsightChangedDto(this OptimizationRunDocument run) =>
        new(
            run.TenantId,
            run.OptimizationRunId,
            run.Version,
            run.UpdatedAtUtc,
            run.AiInsight is not null,
            run.AiInsight?.Status);
}
