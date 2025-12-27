using Planner.BlazorApp.Forms;
using Planner.BlazorApp.Models;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Responses;

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

        await LoadCustomersAsync();
        await LoadVehiclesAsync();
        BuildDefaultDepot();

        await hub.ConnectAsync(tenantId);
        hub.OptimizationCompleted += OnOptimizationCompleted;

        CollectionChanged?.Invoke("Initialized");
        _initialized = true;
    }

    // -----------------------------
    // Data loading
    // -----------------------------

    private async Task LoadCustomersAsync() {
        var baseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return;

        var customers = await http.GetFromJsonAsync<List<Planner.Domain.Customer>>($"{baseUrl}customers") ?? [];

        Customers = customers.Select(c => new CustomerFormModel {
            CustomerId = c.CustomerId,
            Name = c.Name,
            LocationId = c.Location.Id,
            Latitude = c.Location.Latitude,
            Longitude = c.Location.Longitude,
            Address = c.Location.Address,
            DefaultServiceMinutes = c.DefaultServiceMinutes,
            RequiresRefrigeration = c.RequiresRefrigeration,
            DefaultJobType = 1 // Assuming delivery
        }).ToList();

        MapCustomers = Customers.Select(c => new JobMarker {
            Lat = c.Latitude,
            Lng = c.Longitude,
            Label = c.Name
        }).ToList();
    }

    private async Task LoadVehiclesAsync() {
        var baseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return;

        Vehicles = await http.GetFromJsonAsync<List<Vehicle>>($"{baseUrl}vehicles") ?? [];
    }

    private void BuildDefaultDepot() {
        // For now: one depot per tenant
        // Later: load from API
        if (Vehicles.Any()) {
            var depotVehicle = Vehicles.First();
            var depotCustomer = Customers.FirstOrDefault(c => c.Name.Contains("Depot"));

            if (depotCustomer != null) {
                Depots =
                [
                    new DepotInput(new LocationInput(
                        depotVehicle.DepotStartId,
                        depotCustomer.Address,
                        depotCustomer.Latitude,
                        depotCustomer.Longitude
                    ))
                ];
            } else {
                // Fallback if no depot customer is found
                Depots =
                [
                    new DepotInput(new LocationInput(
                        0,
                        "Main Depot",
                        -31.953823,
                        115.876140
                    ))
                ];
            }
        }
    }

    // -----------------------------
    // Solve VRP
    // -----------------------------

    public Task SolveVrp() => SolveVrpAsync(TenantId);

    public async Task SolveVrpAsync(Guid tenantId) {
        var endpoint = "api/vrp/solve";
        if (string.IsNullOrWhiteSpace(endpoint))
            return;

        var response = await http.GetAsync(endpoint);

        if (response.IsSuccessStatusCode) {
            int waitMinutes = Math.Max(1, Jobs.Count / 2);
            StartWait?.Invoke(waitMinutes);
        }
    }

    // -----------------------------
    // SignalR callback
    // -----------------------------

    private void OnOptimizationCompleted(OptimizeRouteResponse evt) {
        Routes = evt.Routes.ToList();
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

