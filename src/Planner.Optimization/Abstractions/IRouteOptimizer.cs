using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Optimization;

public interface IRouteOptimizer {
    OptimizeRouteResponse Optimize(OptimizeRouteRequest request);
}
