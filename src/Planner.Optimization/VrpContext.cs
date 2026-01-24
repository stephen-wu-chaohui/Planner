
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Inputs;

internal sealed record VrpContext(
    OptimizeRouteRequest Request,
    IReadOnlyList<JobInput> Jobs,
    IReadOnlyList<VehicleInput> Vehicles,
    IReadOnlyList<long> Locations,
    long TimeScale,
    long DistanceScale
);
