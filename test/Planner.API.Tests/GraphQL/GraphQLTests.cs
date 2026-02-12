using System.Net.Http.Json;
using System.Text.Json;

namespace Planner.API.Tests.GraphQL;

public class GraphQLTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory> {
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GraphQL_Endpoint_Returns_Schema() {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/graphql?sdl");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("type Query", content);
        Assert.Contains("type Mutation", content);
    }

    [Fact]
    public async Task GraphQL_Query_Type_Contains_All_Targets() {
        var payload = await ExecuteGraphQLAsync(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        var fields = payload
            .GetProperty("data")
            .GetProperty("__type")
            .GetProperty("fields")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var expected = new[] {
            "jobs", "jobById",
            "customers", "customerById",
            "vehicles", "vehicleById",
            "depots", "depotById",
            "locations", "locationById",
            "routes", "routeById",
            "tasks", "taskById"
        };

        expected.Should().OnlyContain(name => fields.Contains(name));
    }

    [Fact]
    public async Task GraphQL_Mutation_Type_Contains_All_Targets() {
        var payload = await ExecuteGraphQLAsync(
            """
            {
              __type(name: "Mutation") {
                fields {
                  name
                }
              }
            }
            """);

        var fields = payload
            .GetProperty("data")
            .GetProperty("__type")
            .GetProperty("fields")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var expected = new[] {
            "createJob", "updateJob", "deleteJob",
            "createCustomer", "updateCustomer", "deleteCustomer",
            "createVehicle", "updateVehicle", "deleteVehicle",
            "createDepot", "updateDepot", "deleteDepot",
            "createLocation", "updateLocation", "deleteLocation",
            "createTask", "updateTask", "deleteTask"
        };

        expected.Should().OnlyContain(name => fields.Contains(name));
    }

    private async Task<JsonElement> ExecuteGraphQLAsync(string query) {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/graphql", new { query });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
