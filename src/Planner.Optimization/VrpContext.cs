using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Requests;

internal sealed record VrpContext(
    OptimizeRouteRequest Request,
    IReadOnlyList<JobInput> Jobs,
    IReadOnlyList<VehicleInput> Vehicles,
    IReadOnlyList<LocationInput> Locations,
    long TimeScale,
    long DistanceScale
);
