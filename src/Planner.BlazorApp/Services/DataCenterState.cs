using Planner.BlazorApp.Forms;
using Planner.BlazorApp.Models;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;
using Planner.Domain;

namespace Planner.BlazorApp.Services;

public sealed class DataCenterState(
    PlannerApiClient api,
    IOptimizationHubClient hub)
{
    // -----------------------------
    // State (Inputs & Outputs only)
    // -----------------------------

    public List<CustomerFormModel> Customers { get; private set; } = [];
    public List<Vehicle> Vehicles { get; private set; } = [];
    public List<JobFormModel> Jobs { get; private set; } = [];
    public List<RouteResult> Routes { get; private set; } = [];

    public LocationInput MapDepotLocation { get; private set; } = new(
        LocationId: 0,
        Address: "Main Depot",
        Latitude: -31.953823,
        Longitude: 115.876140);

    public Guid TenantId { get; private set; }

    // -----------------------------
    // UI helpers
    // -----------------------------

    public List<MapRoute> MapRoutes { get; private set; } = [];
    public List<JobMarker> MapCustomers { get; private set; } = [];

    public event Action<string>? CollectionChanged;
    public event Action<int>? StartWait;

    private bool _initialized;

    public void Reset()
    {
        Customers = [];
        Vehicles = [];
        Jobs = [];
        Routes = [];
        MapRoutes = [];
        MapCustomers = [];

        MapDepotLocation = new LocationInput(
            LocationId: 0,
            Address: "Main Depot",
            Latitude: -31.953823,
            Longitude: 115.876140);

        _initialized = false;
    }

    // -----------------------------
    // Initialization
    // -----------------------------

    public async Task InitializeAsync(Guid tenantId)
    {
        if (_initialized)
            return;

        TenantId = tenantId;

        await LoadCustomersAsync();
        await LoadVehiclesAsync();
        SetMapDepotFromVehicles();

        await hub.ConnectAsync(tenantId);
        hub.OptimizationCompleted += OnOptimizationCompleted;

        CollectionChanged?.Invoke("Initialized");
        _initialized = true;
    }

    // -----------------------------
    // Data loading
    // -----------------------------

    private async Task LoadCustomersAsync()
    {
        var customers = await api.GetFromJsonAsync<List<Customer>>("api/customers") ?? [];

        Customers = customers.Select(c => new CustomerFormModel
        {
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

        MapCustomers = Customers.Select(c => new JobMarker
        {
            Lat = c.Latitude,
            Lng = c.Longitude,
            Label = c.Name
        }).ToList();
    }

    private async Task LoadVehiclesAsync()
    {
        Vehicles = await api.GetFromJsonAsync<List<Vehicle>>("api/vehicles") ?? [];
    }

    private void SetMapDepotFromVehicles()
    {
        var depotLoc = Vehicles
            .Select(v => v.StartDepot?.Location)
            .FirstOrDefault(l => l is not null);

        if (depotLoc is null)
            return;

        MapDepotLocation = new LocationInput(
            LocationId: depotLoc.Id,
            Address: depotLoc.Address,
            Latitude: depotLoc.Latitude,
            Longitude: depotLoc.Longitude);
    }

    // -----------------------------
    // Solve VRP
    // -----------------------------

    public Task SolveVrp() => SolveVrpAsync(TenantId);

    public async Task SolveVrpAsync(Guid tenantId)
    {
        const string endpoint = "api/vrp/solve";
        var settings = await api.GetFromJsonAsync<OptimizationSettings>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0)
        {
            int waitMinutes = (settings.SearchTimeLimitSeconds + 59) / 60;
            StartWait?.Invoke(waitMinutes);
        }
    }

    // -----------------------------
    // SignalR callback
    // -----------------------------

    private void OnOptimizationCompleted(OptimizeRouteResponse evt)
    {
        Routes = evt.Routes.ToList();
        BuildMapRoutes();
        CollectionChanged?.Invoke("Routes");
    }

    private void BuildMapRoutes()
    {
        MapRoutes = Routes.Select(route => new MapRoute
        {
            RouteName = route.VehicleName,
            Color = ColourHelper.ColourFromString(route.VehicleName, 0.95, 0.25) ?? "#FF0000",
            //Points = route.Stops
            //    .Join(Jobs,
            //        stop => stop.JobId,
            //        job => job.JobId,
            //        (stop, job) => job)
            //    .Join(Customers,
            //        job => job.LocationId,
            //        customer => customer.LocationId,
            //        (job, customer) => new JobMarker
            //        {
            //            Lat = customer.Latitude,
            //            Lng = customer.Longitude,
            //            Label = customer.Name
            //        })
            //    .ToList()
            Points = route.Stops.Select(stop =>
                    new JobMarker {
                        Lat = stop.Location.Latitude,
                        Lng = stop.Location.Longitude,
                        Label = stop.Name,
                        JobType = stop.JobType.ToString()
                    })
                .ToList()

        }).ToList();
    }

    // -----------------------------
    // Mutations (UI actions)
    // -----------------------------

    public void AddJob(JobFormModel job)
    {
        Jobs.Add(job);
        CollectionChanged?.Invoke("Jobs");
    }

    public void ClearJobs()
    {
        Jobs.Clear();
        Routes.Clear();
        MapRoutes.Clear();
        CollectionChanged?.Invoke("Jobs");
    }
}

