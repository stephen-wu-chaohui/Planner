using Planner.Contracts.API;

namespace Planner.Contracts.Optimization;

/// <summary>
/// Describes one stop in an optimized vehicle route.
/// </summary>
public sealed record TaskAssignmentDto(
    long JobId,
    double ArrivalTime,
    double DepartureTime,
    long PalletLoad,
    long WeightLoad,
    long RefrigeratedLoad
);
