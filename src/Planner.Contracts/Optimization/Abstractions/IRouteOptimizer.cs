using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;

namespace Planner.Contracts.Optimization.Abstractions;

public interface IRouteOptimizer {
    OptimizeRouteResponse Optimize(OptimizeRouteRequest request);
}
