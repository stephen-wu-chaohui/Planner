namespace Planner.Contracts.Optimization;

/// <summary>
/// Describes one stop in an optimized vehicle route.
/// </summary>
public sealed record TaskAssignmentDto(
    long LocationId,
    double Latitute,
    double Longtitute,
    double ArrivalTime,
    double DepartureTime,
    long PalletLoad,
    long WeightLoad,
    long RefrigeratedLoad,
    string? JobName = null,
    string? JobType = null,
    string? CustomerName = null
);
