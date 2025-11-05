using Planner.Contracts.Helper;
using System.Text.Json;

// ------------------- Helper --------------------
static async Task<double[][]> GetDistanceMatrixAsync(string apiKey, List<(string id, double lat, double lon)> points) {
    using var http = new HttpClient();

    string origins = string.Join("|", points.Select(p => $"{p.lat},{p.lon}"));
    string destinations = origins;
    string url = ""; // $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={apiKey}";

    var json = await http.GetStringAsync(url);
    using var doc = JsonDocument.Parse(json);

    var rows = doc.RootElement.GetProperty("rows");
    int n = points.Count;
    var matrix = new double[n, n];

    for (int i = 0; i < n; i++) {
        var elements = rows[i].GetProperty("elements");
        for (int j = 0; j < n; j++) {
            var status = elements[j].GetProperty("status").GetString();
            if (status == "OK") {
                var dist = elements[j].GetProperty("distance").GetProperty("value").GetDouble(); // meters
                matrix[i, j] = dist / 1000.0; // convert to km
            } else matrix[i, j] = double.PositiveInfinity;
        }
    }

    return MatrixConverter.ToJagged(matrix);
}


