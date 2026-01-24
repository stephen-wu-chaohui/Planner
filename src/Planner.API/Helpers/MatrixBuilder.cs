using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Requests;

namespace Planner.API.Helpers;

/// <summary>
/// Helper class to build distance and travel time matrices from locations.
/// </summary>
public static class MatrixBuilder
{
    /// <summary>
    /// Builds distance and travel time matrices from a list of locations.
    /// </summary>
    /// <param name="locations">List of all locations (depots + jobs) in the same order as they will appear in the solver.</param>
    /// <param name="settings">Optimization settings containing distance/time calculation parameters.</param>
    /// <returns>Tuple of (DistanceMatrix, TravelTimeMatrix).</returns>
    public static (long[][] DistanceMatrix, long[][] TravelTimeMatrix) BuildMatrices(
        IReadOnlyList<LocationInput> locations,
        OptimizationSettings settings)
    {
        int n = locations.Count;
        var distMatrix = new long[n][];
        var travelMatrix = new long[n][];

        for (int i = 0; i < n; i++)
        {
            distMatrix[i] = new long[n];
            travelMatrix[i] = new long[n];

            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    distMatrix[i][j] = 0;
                    travelMatrix[i][j] = 0;
                    continue;
                }

                // Compute distance using Manhattan distance approximation
                double km = settings.KmDegreeConstant *
                    (Math.Abs(locations[i].Latitude - locations[j].Latitude) +
                     Math.Abs(locations[i].Longitude - locations[j].Longitude));

                double minutes = km * settings.TravelTimeMultiplier;

                // Round and store as long
                distMatrix[i][j] = (long)Math.Round(km);
                travelMatrix[i][j] = (long)Math.Round(minutes);
            }
        }

        return (distMatrix, travelMatrix);
    }
}
