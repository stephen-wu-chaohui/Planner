namespace Planner.BlazorApp.FormModels;

public class MapRoute {
    public string RouteName { get; set; } = "";
    public string Color { get; set; } = "blue";
    public string Label { get; set; } = "";
    public List<CustomerMarker> Points { get; set; } = new();
}
