using Planner.BlazorApp.Services;
using System;

namespace Planner.BlazorApp.State.Interfaces;

public interface IInsightState
{
    /// <summary>
    /// Latest route insight from AI analysis.
    /// </summary>
    RouteInsight? LatestInsight { get; }

    /// <summary>
    /// Event raised when insights change.
    /// </summary>
    event Action? OnInsightsChanged;

    /// <summary>
    /// Clears the current insight.
    /// </summary>
    Task ClearInsightAsync();
}
