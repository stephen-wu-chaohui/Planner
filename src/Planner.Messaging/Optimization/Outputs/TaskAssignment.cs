namespace Planner.Messaging.Optimization.Outputs;

/// <summary>
/// Describes one stop in an optimized vehicle route.
/// </summary>
public sealed record TaskAssignment(
    long LocationId,
    double ArrivalTime,
    double DepartureTime,
    long PalletLoad,
    long WeightLoad,
    long RefrigeratedLoad
);
