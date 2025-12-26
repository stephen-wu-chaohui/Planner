namespace Planner.Contracts.Optimization.Inputs;

/// <summary>
/// Represents a depot / starting location for route optimization.
/// Immutable and solver-friendly.
/// </summary>
public sealed record DepotInput(LocationInput Location);
