namespace Planner.Messaging.Optimization.Inputs; 

// Carrying the previously hardcoded "magic numbers"
public record OptimizationSettings(
    long MaxSlackMinutes = 60,
    long HorizonMinutes = 480,
    double KmPerMinute = 0.84,          // 50 km/h
    double OvertimeMultiplier = 2.0,
    double DistanceScale = 1.0,
    int SearchTimeLimitSeconds = 60);

// Update to OptimizeRouteRequest to include Settings
// public record OptimizeRouteRequest(..., OptimizationSettings? Settings = null);
