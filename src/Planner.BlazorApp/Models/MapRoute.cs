namespace Planner.BlazorApp.Models;

public class MapRoute {
    public string RouteName { get; set; } = "";
    public string Color { get; set; } = "blue";
    public string Label { get; set; } = "";
    public List<JobMarker> Points { get; set; } = new();
}
