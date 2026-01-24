
using Planner.Messaging.Optimization;

internal sealed record VrpContext(
    OptimizeRouteRequest Request,
    IReadOnlyList<JobInput> Jobs,
    IReadOnlyList<VehicleInput> Vehicles,
    IReadOnlyList<LocationInput> Locations,
    long TimeScale,
    long DistanceScale
);
