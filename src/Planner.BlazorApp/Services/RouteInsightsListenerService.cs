namespace Planner.BlazorApp.Services;

/// <summary>
/// Model for route insights data from the Planner API.
/// </summary>
public record RouteInsight
{
    public string RequestId { get; init; } = string.Empty;
    public string Analysis { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Status { get; init; } = string.Empty;
}
