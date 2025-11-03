namespace Planner.Contracts.Messages.LinearSolver;

/// <summary>
/// Message published by Planner.API to request a solver run.
/// </summary>
public record LinearSolverRequestMessage {
    public required string RequestId { get; init; }
    public required LinearSolverRequest Request { get; init; }
}
