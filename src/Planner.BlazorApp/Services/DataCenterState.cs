using Planner.BlazorApp.Forms;
using Planner.BlazorApp.Models;
using Planner.Contracts.API;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;
using System;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;

namespace Planner.BlazorApp.Services;

public sealed class DataCenterState(
    PlannerApiClient api,
    IOptimizationHubClient hub)
{
    // -----------------------------
    // State (Inputs & Outputs only)
    // -----------------------------

    public List<CustomerDto> Customers { get; private set; } = [];
    public List<VehicleDto> Vehicles { get; private set; } = [];
    public List<JobDto> Jobs { get; private set; } = [];
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
    public event Action<DataChangedEventArgs<CustomerDto>>? OnCustomerChanged;

    private void NotifyCustomerChanged(CustomerDto customer, DataChangeType type)
        => OnCustomerChanged?.Invoke(new DataChangedEventArgs<CustomerDto> { Item = customer, ChangeType = type });

    private async Task LoadCustomersAsync()
    {
        var customers = await api.GetFromJsonAsync<List<CustomerDto>>("api/customers") ?? [];
        Customers = customers;
        MapCustomers = Customers.Select(c => new JobMarker
        {
            Lat = c.Location.Latitude,
            Lng = c.Location.Longitude,
            Label = c.Name
        }).ToList();
    }

    public async Task AddNewCustomerAsync(CustomerFormModel customer) {
        var dto = customer.ToDto();
        var response = await api.PostAsJsonAsync("api/customers", dto);
        if (response.IsSuccessStatusCode) {
            var result = await response.Content.ReadFromJsonAsync<CustomerDto>();
            // Notify subscribers exactly what happened
            if (result is not null) {
                NotifyCustomerChanged(result, DataChangeType.Added);
            }
        }
    }

    public async Task UpdateCustomerAsync(CustomerFormModel customer) {
        var dto = customer.ToDto();
        var response = await api.PutAsJsonAsync("api/customers", dto);
        if (response.IsSuccessStatusCode) {
            NotifyCustomerChanged(dto, DataChangeType.Updated);
        }
    }

    public async Task RemoveCustomerAsync(CustomerFormModel customer) {
        var response = await api.DeleteAsync("api/customers", customer.CustomerId);
        if (response.IsSuccessStatusCode) {
            var dto = customer.ToDto();
            NotifyCustomerChanged(dto, DataChangeType.Deleted);
        }
    }
    #endregion

    #region Vehicle management
    // -----------------------------
    // Vehicle management
    // -----------------------------
    private async Task LoadVehiclesAsync()
    {
        Vehicles = await api.GetFromJsonAsync<List<VehicleDto>>("api/vehicles") ?? [];
    }

    public async Task AddNewVehicleAsync(VehicleDto vehicle)
    {
        var response = await api.PostAsJsonAsync("api/vehicles", vehicle);
        if (response.IsSuccessStatusCode)
        {
            var newVehicle = await response.Content.ReadFromJsonAsync<VehicleDto>();
            if (newVehicle is not null)
            {
                Vehicles.Add(newVehicle);
            }
            CollectionChanged?.Invoke("Vehicles");
        }
    }

    public async Task UpdateVehicleAsync(VehicleDto vehicle)
    {
        var response = await api.PutAsJsonAsync("api/vehicles", vehicle);
        if (response.IsSuccessStatusCode)
        {
            CollectionChanged?.Invoke("Vehicles");
        }
    }

    public async Task RemoveVehicleAsync(VehicleDto vehicle)
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

    private async void SetMapDepotFromVehicles()
    {
        // Vehicles DTOs do not include depot navigation; fetch a depot for map center.
        var depots = await api.GetFromJsonAsync<List<DepotDto>>("api/depots") ?? [];
        var depot = depots.FirstOrDefault();
        if (depot is null)
            return;

        MapDepotLocation = new LocationInput(
            LocationId: depot.Location.Id,
            Address: depot.Location.Address,
            Latitude: depot.Location.Latitude,
            Longitude: depot.Location.Longitude);
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

    public void AddJob(JobDto job)
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
            Jobs.Add(new JobDto(
                Id: nextId++,
                Name: c.Name,
                OrderId: 0,
                CustomerId: c.CustomerId,
                JobType: JobTypeDto.Delivery,
                Reference: $"JOB-{nextId}",
                Location: c.Location,
                ServiceTimeMinutes: c.DefaultServiceMinutes,
                ReadyTime: 0,
                DueTime: 480,
                PalletDemand: 2,
                WeightDemand: 100,
                RequiresRefrigeration: c.RequiresRefrigeration
            ));
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

