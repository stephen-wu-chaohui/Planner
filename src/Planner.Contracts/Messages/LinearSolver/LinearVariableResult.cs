namespace Planner.Contracts.Messages.LinearSolver;

public record LinearVariableResult
{
    public string Name { get; init; } = string.Empty;
    public double Value { get; init; }
    public double ReducedCost { get; init; }
}
