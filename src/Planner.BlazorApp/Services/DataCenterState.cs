using Planner.BlazorApp.Components.DispatchCenter;
using Planner.BlazorApp.Forms;
using Planner.Application.Optimization.Mappers;
using Planner.Contracts.Messaging.Events;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using static Planner.BlazorApp.Components.DispatchCenter.PlannerMap;

namespace Planner.BlazorApp.Services;

public sealed class DataCenterState(
    HttpClient http,
    IOptimizationHubClient hub,
    IConfiguration configuration) {
    // -----------------------------
    // State (Inputs & Outputs only)
    // -----------------------------

    public List<CustomerFormModel> Customers { get; private set; } = [];
    public List<Vehicle> Vehicles { get; private set; } = [];
    public List<JobFormModel> Jobs { get; private set; } = [];
    public List<RouteResult> Routes { get; private set; } = [];

    public List<DepotInput> Depots { get; private set; } = [];

    public Guid TenantId { get; private set; }

    // -----------------------------
    // UI helpers
    // -----------------------------

    public List<MapRoute> MapRoutes { get; private set; } = [];
    public List<JobMarker> MapCustomers { get; private set; } = [];

    public event Action<string>? CollectionChanged;
    public event Action<int>? StartWait;

    private bool _initialized;

    // -----------------------------
    // Initialization
    // -----------------------------

    public async Task InitializeAsync(Guid tenantId) {
        if (_initialized)
            return;

        TenantId = tenantId;

        //await LoadCustomersAsync();
        //await LoadVehiclesAsync();
        BuildDefaultDepot();

        //await hub.ConnectAsync(tenantId);
        //hub.OptimizationCompleted += OnOptimizationCompleted;

        // CollectionChanged?.Invoke("Initialized");
        _initialized = true;
    }

    // -----------------------------
    // Data loading
    // -----------------------------

    private async Task LoadCustomersAsync() {
        var url = configuration["DataEndpoints:Customers"];
        if (!string.IsNullOrWhiteSpace(url))
            Customers = await http.GetFromJsonAsync<List<CustomerFormModel>>(url) ?? [];

        MapCustomers = Customers.Select(c => new JobMarker {
            Lat = c.Latitude,
            Lng = c.Longitude,
            Label = c.Name
        }).ToList();
    }

    private async Task LoadVehiclesAsync() {
        var url = configuration["DataEndpoints:Vehicles"];
        if (!string.IsNullOrWhiteSpace(url))
            Vehicles = await http.GetFromJsonAsync<List<Vehicle>>(url) ?? [];
    }

    private void BuildDefaultDepot() {
        // For now: one depot per tenant
        // Later: load from API
        Depots =
        [
            new DepotInput(new LocationInput(
                0, // LocationId (matches demo vehicles.json)
                "Main Depot", // Address/label
                -31.953823, // Latitude
                115.876140 // Longitude
            ))
        ];
    }

    // -----------------------------
    // Solve VRP
    // -----------------------------

    public Task SolveVrp() => SolveVrpAsync(TenantId);

    public async Task SolveVrpAsync(Guid tenantId) {
        var jobInputs = JobInputMapper.ToJobInputs(Jobs);
        var vehicleInputs = Vehicles.Select(ToVehicleInput).ToList();

        var request = new OptimizeRouteRequest {
            TenantId = tenantId,
            OptimizationRunId = Guid.NewGuid(),
            Jobs = jobInputs,
            Vehicles = vehicleInputs,
            Depots = Depots
        };

        var response = await http.PostAsJsonAsync(
            configuration["Planner.Api:VRP.Solver.Endpoint"],
            request);

        if (response.IsSuccessStatusCode) {
            int waitMinutes = Math.Max(1, Jobs.Count / 2);
            StartWait?.Invoke(waitMinutes);
        }
    }

    private static VehicleInput ToVehicleInput(Vehicle v) {
        var costPerMinute = (v.DriverRatePerHour + v.MaintenanceRatePerHour) / 60.0;

        return new VehicleInput(
            VehicleId: v.Id,
            Name: v.Name,
            ShiftLimitMinutes: v.ShiftLimitMinutes,
            DepotStartId: v.DepotStartId,
            DepotEndId: v.DepotEndId,
            SpeedFactor: v.SpeedFactor,
            CostPerMinute: costPerMinute,
            CostPerKm: v.FuelRatePerKm,
            BaseFee: v.BaseFee,
            MaxPallets: v.MaxPallets,
            MaxWeight: v.MaxWeight,
            RefrigeratedCapacity: v.RefrigeratedCapacity
        );
    }

    // -----------------------------
    // SignalR callback
    // -----------------------------

    private void OnOptimizationCompleted(RouteOptimizedEvent evt) {
        Routes = evt.Result.Routes.ToList();
        BuildMapRoutes();
        CollectionChanged?.Invoke("Routes");
    }

    private void BuildMapRoutes() {
        MapRoutes = Routes.Select(route => new MapRoute {
            RouteName = route.VehicleName,
            Color = ColourHelper.ColourFromString(route.VehicleName, 0.85, 0.55) ?? "#FF0000",
            Points = route.Stops
                .Join(Jobs,
                    stop => stop.JobId,
                    job => job.JobId,
                    (stop, job) => job)
                .Join(Customers,
                    job => job.LocationId,
                    customer => customer.LocationId,
                    (job, customer) => new JobMarker {
                        Lat = customer.Latitude,
                        Lng = customer.Longitude,
                        Label = customer.Name
                    })
                .ToList()
        }).ToList();
    }

    // -----------------------------
    // Mutations (UI actions)
    // -----------------------------

    public void AddJob(JobFormModel job) {
        Jobs.Add(job);
        CollectionChanged?.Invoke("Jobs");
    }

    public void ClearJobs() {
        Jobs.Clear();
        Routes.Clear();
        MapRoutes.Clear();
        CollectionChanged?.Invoke("Jobs");
    }
}
