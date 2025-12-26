namespace Planner.Contracts.Optimization.Outputs;

/// <summary>
/// Describes one stop in an optimized vehicle route.
/// </summary>
public sealed record TaskAssignment(
    int JobId,
    int JobType,
    string Name,
    double ArrivalTime,
    double DepartureTime,
    long PalletLoad,
    long WeightLoad,
    long RefrigeratedLoad
);
