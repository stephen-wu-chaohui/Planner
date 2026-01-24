namespace Planner.Contracts.Optimization;

/// <summary>
/// Represents the response from a route optimization request.
/// </summary>
/// <param name="TenantId">Multi-tenant security boundary.</param>
/// <param name="OptimizationRunId">Correlation ID for the optimization run.</param>
/// <param name="CompletedAt">UTC timestamp when optimization completed.</param>
/// <param name="Routes">Generated routes.</param>
/// <param name="TotalCost">Total cost associated with the optimization.</param>
public sealed record RoutingResultDto(
    Guid TenantId,
    Guid OptimizationRunId,
    DateTime CompletedAt,
    IReadOnlyList<RouteDto> Routes,
    double TotalCost
) {
    public readonly string? ErrorMessage;
}
