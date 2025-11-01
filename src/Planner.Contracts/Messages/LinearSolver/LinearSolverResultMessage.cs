namespace Planner.Contracts.Messages.LinearSolver;

/// <summary>
/// Message published by Planner.Optimization.Worker after solving a request.
/// </summary>
public record LinearSolverResultMessage
{
    public required string RequestId { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required LinearSolverResponse Response { get; init; }
}
