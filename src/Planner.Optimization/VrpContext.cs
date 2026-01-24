
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Inputs;

internal sealed record VrpContext(
    OptimizeRouteRequest Request,
    IReadOnlyList<JobInput> Jobs,
    IReadOnlyList<VehicleInput> Vehicles,
    long TimeScale,
    long DistanceScale
);
