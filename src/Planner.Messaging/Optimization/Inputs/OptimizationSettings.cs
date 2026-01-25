namespace Planner.Messaging.Optimization.Inputs; 

// Carrying the previously hardcoded "magic numbers"
public record OptimizationSettings(
    long MaxSlackMinutes = 60,
    long HorizonMinutes = 480,
    double KmDegreeConstant = 111.32,
    double TravelTimeMultiplier = 5,
    double OvertimeMultiplier = 2.0,
    double DistanceScale = 1.0,
    int SearchTimeLimitSeconds = 100);

// Update to OptimizeRouteRequest to include Settings
// public record OptimizeRouteRequest(..., OptimizationSettings? Settings = null);
