namespace Planner.Messaging.Optimization.Inputs;

/// <summary>
/// Immutable vehicle definition used for route optimization.
/// Transport-safe and solver-friendly.
/// </summary>
public sealed record VehicleInput(
    long VehicleId,
    long StartDepotLocationId,  // index to input stops for depots
    long EndDepotLocationId,    // index to input stops for depots

    // Availability
    long ShiftLimitMinutes,

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
