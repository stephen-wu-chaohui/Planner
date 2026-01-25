
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Inputs;

internal sealed record VrpContext(
    OptimizeRouteRequest Request,
    IReadOnlyList<StopInput> Jobs,
    IReadOnlyList<VehicleInput> Vehicles,
    long TimeScale,
    long DistanceScale
);
