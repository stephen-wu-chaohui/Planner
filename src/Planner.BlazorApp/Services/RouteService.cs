using Microsoft.AspNetCore.Components;

namespace Planner.BlazorApp.Services;

public sealed class RouteService(HttpClient http) {
    public async Task<IReadOnlyList<RouteView>> GetRoutesAsync(
        Guid optimizationRunId) {
        return await http.GetFromJsonAsync<List<RouteView>>(
            $"/api/optimizations/{optimizationRunId}/routes")
            ?? [];
    }
}

