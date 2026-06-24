namespace Planner.Application.CQRS;

public enum CommandStatus {
    Succeeded,
    NotFound,
    Rejected
}

public sealed record CommandResult(CommandStatus Status, string? Error = null) {
    public static CommandResult Succeeded() => new(CommandStatus.Succeeded);
    public static CommandResult NotFound() => new(CommandStatus.NotFound);
    public static CommandResult Rejected(string error) => new(CommandStatus.Rejected, error);
}

public sealed record CommandResult<TResult>(
    CommandStatus Status,
    TResult? Value = default,
    string? Error = null) {
    public static CommandResult<TResult> Succeeded(TResult value) => new(CommandStatus.Succeeded, value);
    public static CommandResult<TResult> NotFound() => new(CommandStatus.NotFound);
    public static CommandResult<TResult> Rejected(string error) => new(CommandStatus.Rejected, default, error);
}
