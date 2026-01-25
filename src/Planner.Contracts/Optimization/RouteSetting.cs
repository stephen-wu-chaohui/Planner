namespace Planner.Contracts.Optimization;

// Carrying the previously hardcoded "magic numbers"
public record RouteSettings(
    long MaxSlackMinutes = 60,
    long HorizonMinutes = 480,
    double KmDegreeConstant = 111.32,
    double TravelTimeMultiplier = 5,
    int SearchTimeLimitSeconds = 100);
