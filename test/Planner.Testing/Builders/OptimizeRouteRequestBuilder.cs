namespace Planner.Testing.Builders;

public sealed class OptimizeRouteRequestBuilder {
    private Guid _tenantId = TestIds.TenantId;
    private Guid _runId = TestIds.RunId;
    private DateTime _requestedAt = DateTime.UtcNow;

    private double _overtimeMultiplier = 2.0;

    private readonly List<DepotInput> _depots = new();
    private readonly List<JobInput> _jobs = new();
    private readonly List<VehicleInput> _vehicles = new();

    public static OptimizeRouteRequestBuilder Create() => new();

    public OptimizeRouteRequestBuilder WithTenant(Guid tenantId) { _tenantId = tenantId; return this; }
    public OptimizeRouteRequestBuilder WithRunId(Guid runId) { _runId = runId; return this; }
    public OptimizeRouteRequestBuilder WithRequestedAt(DateTime utc) { _requestedAt = utc; return this; }

    public OptimizeRouteRequestBuilder WithOvertimeMultiplier(double value) { _overtimeMultiplier = value; return this; }

    public OptimizeRouteRequestBuilder AddDepot(DepotInput depot) { _depots.Add(depot); return this; }
    public OptimizeRouteRequestBuilder AddJob(JobInput job) { _jobs.Add(job); return this; }
    public OptimizeRouteRequestBuilder AddVehicle(VehicleInput vehicle) { _vehicles.Add(vehicle); return this; }

    public OptimizeRouteRequestBuilder WithDepots(IEnumerable<DepotInput> depots) { _depots.Clear(); _depots.AddRange(depots); return this; }
    public OptimizeRouteRequestBuilder WithJobs(IEnumerable<JobInput> jobs) { _jobs.Clear(); _jobs.AddRange(jobs); return this; }
    public OptimizeRouteRequestBuilder WithVehicles(IEnumerable<VehicleInput> vehicles) { _vehicles.Clear(); _vehicles.AddRange(vehicles); return this; }

    public OptimizeRouteRequest Build() => new(
        _tenantId,
        _runId,
        _requestedAt,
        _vehicles.ToList(),
        _jobs.ToList(),
        _depots.ToList(),
        _overtimeMultiplier
    );
}
