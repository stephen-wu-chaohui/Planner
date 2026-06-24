using Planner.Contracts.Optimization;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Application.OptimizationRuns;

public interface IRouteEnrichmentService {
    Task<RoutingResultDto> EnrichAsync(OptimizeRouteResponse response);
}
