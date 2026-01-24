using Planner.Contracts.Optimization.Requests;

namespace Planner.Testing.Builders;

public sealed class OptimizeRouteRequestBuilder {
    private Guid _tenantId = TestIds.TenantId;
    private Guid _runId = TestIds.RunId;
    private DateTime _requestedAt = DateTime.UtcNow;

    private double _overtimeMultiplier = 2.0;

    private OptimizationSettings _optimizationSettings = new OptimizationSettings {
        SearchTimeLimitSeconds = 5
    };

    private readonly List<JobInput> _jobs = new();
    private readonly List<VehicleInput> _vehicles = new();

    public static OptimizeRouteRequestBuilder Create() => new();

    public OptimizeRouteRequestBuilder WithOptimizationSettings(
        OptimizationSettings settings) {

        _optimizationSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        return this;
    }

    public OptimizeRouteRequestBuilder WithSearchTimeLimitSeconds(int seconds) {
        _optimizationSettings = _optimizationSettings with {
            SearchTimeLimitSeconds = seconds
        };
        return this;
    }
    public OptimizeRouteRequestBuilder WithTenant(Guid tenantId) { _tenantId = tenantId; return this; }
    public OptimizeRouteRequestBuilder WithRunId(Guid runId) { _runId = runId; return this; }
    public OptimizeRouteRequestBuilder WithRequestedAt(DateTime utc) { _requestedAt = utc; return this; }

    public OptimizeRouteRequestBuilder WithOvertimeMultiplier(double value) { _overtimeMultiplier = value; return this; }

    public OptimizeRouteRequestBuilder AddJob(JobInput job) { _jobs.Add(job); return this; }
    public OptimizeRouteRequestBuilder AddVehicle(VehicleInput vehicle) { _vehicles.Add(vehicle); return this; }

    public OptimizeRouteRequestBuilder WithJobs(IEnumerable<JobInput> jobs) { _jobs.Clear(); _jobs.AddRange(jobs); return this; }
    public OptimizeRouteRequestBuilder WithVehicles(IEnumerable<VehicleInput> vehicles) { _vehicles.Clear(); _vehicles.AddRange(vehicles); return this; }

    public OptimizeRouteRequest Build() {
        // Build matrices from locations
        var depotLocations = _vehicles
            .SelectMany(v => new[] { v.StartLocation, v.EndLocation })
            .GroupBy(l => l.LocationId)
            .Select(g => g.First())
            .ToList();
        
        var allLocations = depotLocations.Concat(_jobs.Select(j => j.Location)).ToList();
        var (distanceMatrix, travelTimeMatrix) = BuildMatrices(allLocations, _optimizationSettings);

        return new(
            _tenantId,
            _runId,
            _requestedAt,
            _vehicles.ToList(),
            _jobs.ToList(),
            distanceMatrix,
            travelTimeMatrix,
            _overtimeMultiplier,
            _optimizationSettings
        );
    }

    private static (long[][] DistanceMatrix, long[][] TravelTimeMatrix) BuildMatrices(
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

                double km = settings.KmDegreeConstant *
                    (Math.Abs(locations[i].Latitude - locations[j].Latitude) +
                     Math.Abs(locations[i].Longitude - locations[j].Longitude));

                double minutes = km * settings.TravelTimeMultiplier;

                distMatrix[i][j] = (long)Math.Round(km);
                travelMatrix[i][j] = (long)Math.Round(minutes);
            }
        }

        return (distMatrix, travelMatrix);
    }
}
