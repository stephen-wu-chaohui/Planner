using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Domain.Entities;

namespace Planner.BlazorApp.Services;

public class DataCenterService(HttpClient http, IMessageHubClient Hub) {
    public List<Customer> Customers { get; private set; } = new();
    public List<Vehicle> Vehicles { get; private set; } = new();

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
        Customers = await http.GetFromJsonAsync<List<Customer>>("https://localhost:7014/data/customers.json")
                    ?? new();
    }

    public async Task LoadVehiclesAsync() {
        Vehicles = await http.GetFromJsonAsync<List<Vehicle>>("https://localhost:7014/data/vehicles.json")
                    ?? new();
    }

    // Future extension points:
    // public async Task SaveCustomersAsync() => ...
    // public async Task<ApiResponse> SyncJobsAsync() => ...
    // public IMessageHubClient MessageHub => ...
}
