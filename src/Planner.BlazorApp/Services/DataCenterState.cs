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


    #region Customers management
    // -----------------------------
    // Customers management
    // -----------------------------
    //  OnCustomerChanged -- notify subscribers of changes
    //  LoadCustomersAsync -- load customers from API
    //  AddNewCustomerAsync -- add a new customer via API
    //  UpdateCustomerAsync -- update an existing customer via API
    //  RemoveCustomerAsync -- remove a customer via API
    // -----------------------------
    public event Action<DataChangedEventArgs<Customer>> OnCustomerChanged;

    private void NotifyCustomerChanged(Customer customer, DataChangeType type)
            => OnCustomerChanged?.Invoke(new DataChangedEventArgs<Customer> { Item = customer, ChangeType = type });

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

    public async Task AddNewCustomerAsync(CustomerFormModel customer) {
        var customerCto = customer.ToContract();
        var response = await api.PostAsJsonAsync("api/customers", customerCto);
        if (response.IsSuccessStatusCode) {
            var result = await response.Content.ReadFromJsonAsync<Customer>();
            // Notify subscribers exactly what happened
            if (result is not null) {
                NotifyCustomerChanged(result, DataChangeType.Added);
            }
        }
    }

    public async Task UpdateCustomerAsync(CustomerFormModel customer) {
        var customerCto = customer.ToContract();
        var response = await api.PutAsJsonAsync("api/customers", customerCto);
        if (response.IsSuccessStatusCode) {
            NotifyCustomerChanged(customerCto, DataChangeType.Updated);
        }
    }

    public async Task RemoveCustomerAsync(CustomerFormModel customer) {
        var customerCto = customer.ToContract();
        var response = await api.DeleteAsync("api/customers", customer.CustomerId);
        if (response.IsSuccessStatusCode) {
            NotifyCustomerChanged(customerCto, DataChangeType.Deleted);
        }
    }
    #endregion

    #region Vehicle management
    // -----------------------------
    // Vehicle management
    // -----------------------------
    private async Task LoadVehiclesAsync()
    {
        Vehicles = await api.GetFromJsonAsync<List<Vehicle>>("api/vehicles") ?? [];
    }

    public async Task AddNewVehicleAsync(Vehicle vehicle)
    {
        var response = await api.PostAsJsonAsync("api/vehicles", vehicle);
        if (response.IsSuccessStatusCode)
        {
            var newVehicle = await response.Content.ReadFromJsonAsync<Vehicle>();
            if (newVehicle is not null)
            {
                Vehicles.Add(newVehicle);
            }
            CollectionChanged?.Invoke("Vehicles");
        }
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var response = await api.PutAsJsonAsync("api/vehicles", vehicle);
        if (response.IsSuccessStatusCode)
        {
            CollectionChanged?.Invoke("Vehicles");
        }
    }

    public async Task RemoveVehicleAsync(Vehicle vehicle)
    {
        var response = await api.DeleteAsync("api/vehicles", vehicle.Id);
        if (response.IsSuccessStatusCode)
        {
            Vehicles.RemoveAll(v => v.Id == vehicle.Id);
            CollectionChanged?.Invoke("Vehicles");
        }
    }

    #endregion

    #region Depot management and mapping

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
    
    #endregion

    #region Jobs management

    public void AddJob(JobFormModel job)
    {
        Jobs.Add(job);
        CollectionChanged?.Invoke("Jobs");
    }

    public void EnsureJobsFromCustomers()
    {
        if (Customers.Count == 0 || Jobs.Count > 0)
            return;

        Jobs.Clear();

        var nextId = 0;
        foreach (var c in Customers)
        {
            Jobs.Add(new JobFormModel {
                JobId = nextId++,
                JobType = 1,
                Name = c.Name,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                LocationId = c.LocationId,
                ServiceTimeMinutes = c.DefaultServiceMinutes,
                ReadyTime = 0,
                DueTime = 480,
                PalletDemand = 2,
                WeightDemand = 100,
                RequiresRefrigeration = c.RequiresRefrigeration
            });
        }

        CollectionChanged?.Invoke("Jobs");
    }

    public void ClearJobs()
    {
        Jobs.Clear();
        Routes.Clear();
        MapRoutes.Clear();
        CollectionChanged?.Invoke("Jobs");
    }

    #endregion
}

