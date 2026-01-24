
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Responses;

namespace Planner.Optimization;

public interface IRouteOptimizer {
    OptimizeRouteResponse Optimize(OptimizeRouteRequest request);
}
