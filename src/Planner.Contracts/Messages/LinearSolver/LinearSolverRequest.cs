namespace Planner.Contracts.Messages.LinearSolver;

public record LinearSolverRequest
{
    public string Algorithm { get; init; } = "CBC_MIXED_INTEGER_PROGRAMMING";
    public List<LinearVariable> Variables { get; init; } = new();
    public List<LinearExpression> Objectives { get; init; } = new();
    public List<LinearExpression> Constraints { get; init; } = new();
    public LinearSolverParameters? Parameters { get; init; }
}
