using Planner.Application.Messaging;
using Planner.BlazorApp.Components.DispatchCenter;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Domain.Entities;
using static Planner.BlazorApp.Components.DispatchCenter.PlannerMap;

namespace Planner.BlazorApp.Services;

public class DataCenterService(HttpClient http, IMessageHubClient Hub, IConfiguration configuration) {
    public List<Customer> Customers { get; private set; } = [];
    public List<Vehicle> Vehicles { get; private set; } = [];
    public List<Job> Jobs { get; private set; } = [];
    public List<VehicleRoute> Routes { get; private set; } = [];
    public (double Latitude, double Longitude) Depot = (-31.953823, 115.876140);

    public event Action<string>? CollectionChanged; // Optional: notify UI when data refreshed
    public event Action<VrpResultMessage>? VrpResultReceived;
    public event Action<int>? StartWait;


    private bool _initialized;

    public async Task InitializeAsync() {
        if (_initialized)
            return; // already done
        await SetupHubSubscriptionsAsync();
        await LoadCustomersAsync();
        await LoadVehiclesAsync();
        CollectionChanged?.Invoke("Customers and Vehicles");

        _initialized = true;
    }


    private async Task SetupHubSubscriptionsAsync() {
        try {
            await Hub.SubscribeAsync<VrpResultMessage>(
                MessageRoutes.VRPSolverResult,
                msg => {
                    Routes = msg.Result.Routes;
                    WhenRoutesChanged();
                    CollectionChanged?.Invoke("Routes");
                    VrpResultReceived?.Invoke(msg);
                }
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
        WhenCustomersChanged();
    }

    public async Task LoadVehiclesAsync() {
        var url = configuration["DataEndpoints:Vehicles"] ?? "";
        if (!string.IsNullOrEmpty(url)) {
            Vehicles = await http.GetFromJsonAsync<List<Vehicle>>(url) ?? [];
        }
    }

    public List<MapRoute> MapRoutes = [];

    private double saturation = 0.85;
    private double lightness = 0.55;

    private void WhenRoutesChanged() {

        MapRoutes = Routes.Select(r => new MapRoute {
            RouteName = r.VehicleName,
            Color = ColourHelper.ColourFromString(r.VehicleName, saturation, lightness) ?? "#FF0000",
            Points = r.Stops
            .Join(Jobs, s => s.JobId, j => j.Id, (s, j) => new { s, j })
            .Join(Customers, sj => sj.j.CustomerId, c => c.Id,
                (sj, c) => new MapMarker {
                    Lat = c.Latitude,
                    Lng = c.Longitude,
                    Label = c.Name
                })
            .ToList()
        }).ToList();
    }

    public List<MapMarker> MapCustomers = [];

    private void WhenCustomersChanged() {
        MapCustomers = Customers.Select(c => new MapMarker {
            Lat = c.Latitude,
            Lng = c.Longitude,
            Label = c.Name
        }).ToList();
    }

    public void AddNewCustomer(Customer c) {
        c.Id = Customers.Max(c => c.Id) + 1;
        Customers.Add(c);
        WhenCustomersChanged();
        Routes = [];
        WhenRoutesChanged();
        Jobs = [];
    }


    // --- Solve VRP trigger (called from JobsTab) ---
    public async Task SolveVrp() {
        var vrpJobs = Jobs.Prepend(new Job {
            Id = 0,
            CustomerId = -1,
            Name = "Depot",
            Type = JobType.Depot
        });

        var MakeDistanceMatrix = () => {
            var size = vrpJobs.Count(); // +1 for depot
            var points = vrpJobs.Select(j => Customers.FirstOrDefault(c => c.Id == j.CustomerId) is { } customer ? (customer.Latitude, customer.Longitude) : Depot).ToArray();

            var matrix = new double[size][];
            var rand = new Random();
            for (int i = 0; i < size; i++) {
                matrix[i] = new double[size];
                for (int j = 0; j < size; j++) {
                    if (i == j)
                        matrix[i][j] = 0;
                    else
                        matrix[i][j] = 111.3200 * Math.Abs(points[i].Latitude - points[j].Latitude) + Math.Abs(points[i].Longitude - points[j].Longitude); // Manhattan distance
                }
            }
            return matrix;
        };

        var MakeTravelMinutes = MakeDistanceMatrix().Select(row => row.Select(distance => distance * 2).ToArray()).ToArray();   // 30KPH average speed

        var request = new VrpRequestMessage {
            RequestId = Guid.NewGuid(),
            Request = new VrpRequest {
                Jobs = vrpJobs.ToList(),
                Vehicles = Vehicles,
                DistanceKm = MakeDistanceMatrix(),
                TravelMinutes = MakeTravelMinutes,
            },
            CompletedAt = DateTime.UtcNow
        };

        using var client = new HttpClient { BaseAddress = new Uri(configuration["Planner.Api:BaseUrl"]!) };
        var response = await client.PostAsJsonAsync(configuration["Planner.Api:VRP.Solver.Endpoint"], request);
        if (response.IsSuccessStatusCode) {
            Console.WriteLine($"[{DateTime.Now}] VRP request successfully sent.  Please wait for a few minutes to get the result.");
            int waitingMinutes = (int)Math.Ceiling(Math.Pow(vrpJobs.Count(), 2) / 900.0);
            StartWait?.Invoke(waitingMinutes);
        } else {
            Console.WriteLine($"[{DateTime.Now}] VRP request failed to sent.  Please check the connection.");
        }
    }

}
