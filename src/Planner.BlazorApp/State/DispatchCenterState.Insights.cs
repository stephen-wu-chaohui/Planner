using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.OptimizationRuns;
using System.Net.Http.Json;

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

    private void HandleNewInsight(RouteInsight insight)
    {
        _latestInsight = insight;
        OnNewInsightReceived?.Invoke();
        OnInsightsChanged?.Invoke();
    }

    private async Task RefreshAiInsightAsync(Guid optimizationRunId)
    {
        try
        {
            using var response = await api.GetAsync(
                $"/api/vrp/runs/{optimizationRunId}/insight");

            if (response.StatusCode is System.Net.HttpStatusCode.Accepted
                or System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.NoContent)
            {
                return;
            }

            response.EnsureSuccessStatusCode();

            var insight = await response.Content.ReadFromJsonAsync<OptimizationAiInsightDto>();
            if (insight is null)
            {
                return;
            }

            HandleNewInsight(new RouteInsight
            {
                RequestId = optimizationRunId.ToString(),
                Analysis = insight.AnalysisMarkdown ?? insight.ErrorMessage ?? string.Empty,
                Status = insight.Status,
                Timestamp = insight.UpdatedAtUtc
            });
        }
        catch (AccessTokenNotAvailableException)
        {
            throw;
        }
        catch
        {
            // SignalR is only a notification path; failure to refresh an insight should not break route updates.
        }
    }

    /// <summary>
    /// Clears the current insight.
    /// </summary>
    public async Task ClearInsightAsync()
    {
        _latestInsight = null;
        OnInsightsChanged?.Invoke();
        await Task.CompletedTask;
    }
}
