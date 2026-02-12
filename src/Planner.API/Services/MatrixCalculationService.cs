
using DomainLocation = Planner.Domain.Location;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Services;

/// <summary>
/// Service for calculating distance and travel time matrices for optimization.
/// </summary>
public interface IMatrixCalculationService {
    /// <summary>
    /// Builds distance and travel time matrices from location data.
    /// </summary>
    /// <param name="locations">Ordered list of locations (depots first, then jobs).</param>
    /// <param name="settings">Optimization settings containing calculation parameters.</param>
    /// <param name="timeScale">Scale factor for time values.</param>
    /// <param name="distanceScale">Scale factor for distance values.</param>
    /// <returns>Tuple containing distance matrix and travel time matrix.</returns>
    (long[] DistanceMatrix, long[] TravelTimeMatrix) BuildMatrices(
        IReadOnlyList<DomainLocation> locations,
        OptimizationSettings settings,
        long timeScale = 1,
        long distanceScale = 1);
}

public sealed class MatrixCalculationService : IMatrixCalculationService {
    const double kmDegreeConstant = 111.32;   // Approximate conversion factor

    public (long[] DistanceMatrix, long[] TravelTimeMatrix) BuildMatrices(
        IReadOnlyList<DomainLocation> locations,
        OptimizationSettings settings,
        long timeScale = 1,
        long distanceScale = 1) {

        int n = locations.Count;
        var dist = new long[n * n];
        var travel = new long[n * n];

        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                if (i == j) continue;

                // Compute in double
                double km =
                    kmDegreeConstant *
                    (Math.Abs(locations[i].Latitude - locations[j].Latitude)
                   + Math.Abs(locations[i].Longitude - locations[j].Longitude));

                double minutes = km / settings.KmPerMinute;

                // Scale ONCE, store as long
                dist[i * n + j] = (long)Math.Round(km * distanceScale);
                travel[i * n + j] = (long)Math.Round(minutes * timeScale);
            }
        }

        return (dist, travel);
    }
}
