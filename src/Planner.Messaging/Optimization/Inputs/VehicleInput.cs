using Planner.Messaging.Optimization.Inputs;

namespace Planner.Messaging.Optimization;

/// <summary>
/// Immutable vehicle definition used for route optimization.
/// Transport-safe and solver-friendly.
/// </summary>
public sealed record VehicleInput(
    long VehicleId,

    // Availability
    long ShiftLimitMinutes,
    long StartLocation,
    long EndLocation,

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
