namespace Planner.Contracts.Optimization.Inputs;

/// <summary>
/// Solver-facing job definition.
/// Immutable, serializable, and safe to share across API, Worker, and UI.
/// </summary>
public sealed record JobInput(
    int JobId,                 // Internal reference (stable within request)
    int JobType,               // 0: Depot, 1: Delivery, 2: Pickup
    string Name,

    // Solver-friendly location reference
    LocationInput Location,

    // Service constraints
    long ServiceTimeMinutes,
    long ReadyTime,
    long DueTime,

    // Capacity demands
    long PalletDemand,
    long WeightDemand,

    // Special constraints
    bool RequiresRefrigeration
);
