using Planner.Contracts.Messages.LinearSolver;

namespace Planner.BlazorApp.DataRepresentation;

public record LinearSolverResultRow(LinearSolverResultMessage Message)
{
    public string? RequestId => Message.RequestId;
    public string? Status => Message.Response.Status;
    public double ObjectiveValue => Message.Response.ObjectiveValue;
    public DateTime CompletedAt => Message.CompletedAt;
}
