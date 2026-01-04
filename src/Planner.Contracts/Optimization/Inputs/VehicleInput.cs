namespace Planner.Contracts.Optimization.Inputs;

/// <summary>
/// Immutable vehicle definition used for route optimization.
/// Transport-safe and solver-friendly.
/// </summary>
public sealed record VehicleInput(
    long VehicleId,
    string Name,

    // Availability
    long ShiftLimitMinutes,
    LocationInput StartLocation,
    LocationInput EndLocation,

    // Performance
    double SpeedFactor,

    // TotalCost model (flattened)
    double CostPerMinute,
    double CostPerKm,
    double BaseFee,

    // Capacity constraints
    long MaxPallets,
    long MaxWeight,
    long RefrigeratedCapacity
);
