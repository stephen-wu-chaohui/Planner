
using Planner.API.Services;
using Planner.Domain;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Requests;

namespace Planner.Testing.Builders;

public sealed class OptimizeRouteRequestBuilder {
    private Guid _tenantId = TestIds.TenantId;
    private Guid _runId = TestIds.RunId;
    private DateTime _requestedAt = DateTime.UtcNow;

    private double _overtimeMultiplier = 2.0;

    private OptimizationSettings _optimizationSettings = new() {
        SearchTimeLimitSeconds = 5
    };

    private readonly List<Job> _jobs = new();
    private readonly List<Vehicle> _vehicles = new();

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

    public OptimizeRouteRequestBuilder AddJob(Job job) { _jobs.Add(job); return this; }
    public OptimizeRouteRequestBuilder AddVehicle(Vehicle vehicle) { _vehicles.Add(vehicle); return this; }

    public OptimizeRouteRequestBuilder WithJobs(IEnumerable<Job> jobs) { _jobs.Clear(); _jobs.AddRange(jobs); return this; }
    public OptimizeRouteRequestBuilder WithVehicles(IEnumerable<Vehicle> vehicles) { _vehicles.Clear(); _vehicles.AddRange(vehicles); return this; }
    public OptimizeRouteRequest Build() {
        // Build matrices from locations
        var depotLocations = _vehicles
            .SelectMany(v => new[] { v.StartDepot!.Location, v.EndDepot!.Location })
            .GroupBy(l => l.Id)
            .Select(g => g.First())
            .ToList();
        
        var allLocations = depotLocations.Concat(_jobs.Select(j => j.Location)).ToList();
        var (distanceMatrix, travelTimeMatrix) = MatrixBuilder.BuildMatrices(allLocations, _optimizationSettings);

        return new(
            _tenantId,
            _runId,
            _requestedAt,
            _vehicles.Select(ToInput.ToVehicleInput).ToList(),
            _jobs.Select(ToInput.ToJobInput).ToList(),
            distanceMatrix,
            travelTimeMatrix,
            _overtimeMultiplier,
            _optimizationSettings
        );
    }
}
