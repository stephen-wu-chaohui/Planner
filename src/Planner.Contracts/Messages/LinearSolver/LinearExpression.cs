namespace Planner.Contracts.Messages.LinearSolver;

public record LinearExpression {
    public double[] Coefficients { get; init; } = [];
    public double? LowerBound { get; init; }
    public double? UpperBound { get; init; }
    public string? Name { get; init; }
    public LinearSolverDirection? Direction { get; init; }
    public double Weight { get; init; } = 1.0;
    public string? Tag { get; init; }
    public bool IsActive { get; init; } = true;
}
