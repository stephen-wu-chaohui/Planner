namespace Planner.Contracts.Messages.LinearSolver;

public record LinearSolverResponse
{
    public string Status { get; init; } = "";
    public double ObjectiveValue { get; init; }
    public List<LinearVariableResult> Variables { get; init; } = [];
    public List<LinearConstraintResult> Constraints { get; init; } = [];
}
