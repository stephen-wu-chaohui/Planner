namespace Planner.Contracts.Messages.LinearSolver;

public record LinearConstraintResult {
    public string Name { get; init; } = string.Empty;
    public string? Tag { get; init; }
    public double LhsValue { get; init; }
    public double Slack { get; init; }
    public double DualValue { get; init; }
}
