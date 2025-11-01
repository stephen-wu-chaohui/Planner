namespace Planner.Contracts.Messages.LinearSolver;

public record LinearSolverParameters
{
    public int? TimeLimitMs { get; init; }
    public double? Tolerance { get; init; }
    public bool EnableOutput { get; init; } = false;
    public int? RandomSeed { get; init; }
    public int? NumThreads { get; init; }
    public int? IterationLimit { get; init; }
}
