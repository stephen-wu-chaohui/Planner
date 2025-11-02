using Microsoft.Extensions.Configuration;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Optimization.Solvers;
using System.Text.Json;

// Load configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiKey = config["GoogleMaps:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ Missing GoogleMaps.ApiKey in appsettings.json");
    return;
}

// Define depot and jobs
var depot = new DepotDto {
    Id = "Depot",
    Latitude = -31.9505,
    Longitude = 115.8605
};

var jobs = new List<JobDto>
{
    new() { Id = "J1", Latitude = -31.9783, Longitude = 115.8180 },
    new() { Id = "J2", Latitude = -32.0671, Longitude = 115.8957 },
    new() { Id = "J3", Latitude = -31.9778, Longitude = 115.9450 },
    new() { Id = "J4", Latitude = -32.0469, Longitude = 115.8420 }
};

var vehicles = new List<VehicleDto>
{
    new() { Id = "Truck-1" },
    new() { Id = "Truck-2" }
};

// Build full coordinate list (Depot + Jobs)
var allPoints = new List<(string id, double lat, double lon)>
{
    (depot.Id, depot.Latitude, depot.Longitude)
};
allPoints.AddRange(jobs.Select(j => (j.Id, j.Latitude, j.Longitude)));

// Step 1: Get distance matrix from Google Maps API
Console.WriteLine("📡 Requesting distance matrix...");
var distanceMatrix = await GetDistanceMatrixAsync(apiKey, allPoints);
Console.WriteLine("✅ Distance matrix loaded.");

// Step 2: Solve VRP
var solver = new VrpSolver();
var request = new VrpRequestMessage {
    Depot = depot,
    Jobs = jobs,
    Vehicles = vehicles,
    DistanceMatrix = distanceMatrix
};

var result = solver.Solve(request);

// Step 3: Print results
Console.WriteLine("\n🧮 VRP Solution:");
foreach (var route in result.Vehicles)
{
    Console.WriteLine($"🚚 {route.VehicleId}: {string.Join(" → ", route.Stops.Select(s => s.JobId))}");
    Console.WriteLine($"   Distance: {route.RouteDistance:F2} km\n");
}

Console.WriteLine($"Total Distance: {result.TotalDistance:F2} km");
Console.WriteLine($"Solver Status: {result.SolverStatus}");

return;

// ------------------- Helper --------------------
static async Task<double[,]> GetDistanceMatrixAsync(string apiKey, List<(string id, double lat, double lon)> points)
{
    using var http = new HttpClient();

    string origins = string.Join("|", points.Select(p => $"{p.lat},{p.lon}"));
    string destinations = origins;
    string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={apiKey}";

    var json = await http.GetStringAsync(url);
    using var doc = JsonDocument.Parse(json);

    var rows = doc.RootElement.GetProperty("rows");
    int n = points.Count;
    var matrix = new double[n, n];

    for (int i = 0; i < n; i++)
    {
        var elements = rows[i].GetProperty("elements");
        for (int j = 0; j < n; j++)
        {
            var status = elements[j].GetProperty("status").GetString();
            if (status == "OK")
            {
                var dist = elements[j].GetProperty("distance").GetProperty("value").GetDouble(); // meters
                matrix[i, j] = dist / 1000.0; // convert to km
            } else matrix[i, j] = double.PositiveInfinity;
        }
    }

    return matrix;
}


