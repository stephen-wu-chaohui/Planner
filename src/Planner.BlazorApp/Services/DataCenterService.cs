using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Domain.Entities;

namespace Planner.BlazorApp.Services;

public class DataCenterService(HttpClient http, IMessageHubClient Hub, IConfiguration configuration) {
    public List<Customer> Customers { get; private set; } = [];
    public List<Vehicle> Vehicles { get; private set; } = [];

    public event Action? DataLoaded; // Optional: notify UI when data refreshed
    public event Action<VrpResultMessage>? VrpResultReceived;

    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task InitializeAsync() {
        await _initLock.WaitAsync();
        try {
            if (_initialized)
                return; // already done
            await LoadCustomersAsync();
            await LoadVehiclesAsync();
            DataLoaded?.Invoke();
            await SetupHubSubscriptionsAsync();
            _initialized = true;
        } finally {
            _initLock.Release();
        }
    }


    private async Task SetupHubSubscriptionsAsync() {
        try {
            await Hub.SubscribeAsync<VrpResultMessage>(
                MessageRoutes.VRPSolverResult,
                msg => VrpResultReceived?.Invoke(msg)
            );
            Console.WriteLine("🔌 Hub subscriptions established.");
        } catch (Exception ex) {
            Console.Error.WriteLine($"❌ Failed to subscribe to hub: {ex.Message}");
        }
    }

    public async Task LoadCustomersAsync() {
        var url = configuration["DataEndpoints:Customers"] ?? "";
        if (!string.IsNullOrEmpty(url)) {
            Customers = await http.GetFromJsonAsync<List<Customer>>(url) ?? [];
        }
    }

    public async Task LoadVehiclesAsync() {
        var url = configuration["DataEndpoints:Vehicles"] ?? "";
        if (!string.IsNullOrEmpty(url)) {
            Vehicles = await http.GetFromJsonAsync<List<Vehicle>>(url) ?? [];
        }
    }

    // Future extension points:
    // public async Task SaveCustomersAsync() => ...
    // public async Task<ApiResponse> SyncJobsAsync() => ...
    // public IMessageHubClient MessageHub => ...
}
