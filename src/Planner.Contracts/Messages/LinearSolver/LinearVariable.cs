namespace Planner.Contracts.Messages.LinearSolver;

public record LinearVariable
{
    public string Name { get; init; } = string.Empty;
    public double LowerBound { get; init; } = 0.0;
    public double UpperBound { get; init; } = double.PositiveInfinity;
    public bool IsInteger { get; init; } = false;
}
