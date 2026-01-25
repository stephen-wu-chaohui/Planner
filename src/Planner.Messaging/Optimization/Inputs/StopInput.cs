namespace Planner.Messaging.Optimization.Inputs;

/// <summary>
/// Solver-facing job definition.
/// Immutable, serializable, and safe to share across API, Worker, and UI.
/// </summary>
public sealed record StopInput(
    long LocationId,                // Internal reference (stable within request)
    int LocationType,               // 0: Depot, 1: Delivery, 2: Pickup

    // Service constraints
    long ServiceTimeMinutes,
    long ReadyTime,
    long DueTime,

    // Capacity demands
    long PalletDemand,
    long WeightDemand,

    // Special constraints
    bool RequiresRefrigeration,

    // Extra data used by UI but not solver
    long ? ExtraIdForJob = null
);
