namespace Planner.BlazorApp.Components;

public class MapMarker {
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Label { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public string RouteName { get; set; } = string.Empty;
    public double Arrival { get; set; }
    public double Departure { get; set; }
    public long PalletLoad { get; set; }
    public long WeightLoad { get; set; }
    public long RefrigeratedLoad { get; set; }
}


