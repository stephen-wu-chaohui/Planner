using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IInsightState
{
    private RouteInsight? _latestInsight;
    
    /// <summary>
    /// Latest route insight from AI analysis.
    /// </summary>
    public RouteInsight? LatestInsight => _latestInsight;
    
    /// <summary>
    /// Event raised when a new route insight is received.
    /// </summary>
    public event Action? OnNewInsightReceived;
    
    /// <summary>
    /// Event raised when insights change.
    /// </summary>
    public event Action? OnInsightsChanged;
    
    /// <summary>
    /// Handles new insights from Firestore.
    /// </summary>
    private void HandleNewInsight(RouteInsight insight)
    {
        _latestInsight = insight;
        OnNewInsightReceived?.Invoke();
        OnInsightsChanged?.Invoke();
    }
    
    /// <summary>
    /// Clears the current insight.
    /// </summary>
    public async Task ClearInsightAsync()
    {
        _latestInsight = null;
        OnInsightsChanged?.Invoke();
    }
}

