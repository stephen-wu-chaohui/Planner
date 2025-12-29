namespace Planner.Contracts.Optimization.Requests;

// Carrying the previously hardcoded "magic numbers"
public record OptimizationSettings(
    long MaxSlackMinutes = 60,
    long HorizonMinutes = 720,
    double KmDegreeConstant = 111.32,
    double TravelTimeMultiplier = 2.0,
    int SearchTimeLimitSeconds = 1
);

// Update to OptimizeRouteRequest to include Settings
// public record OptimizeRouteRequest(..., OptimizationSettings? Settings = null);
