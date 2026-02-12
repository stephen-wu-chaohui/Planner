using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Planner.API.Tests.GraphQL;

public class GraphQLTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> _factory;

    public GraphQLTests(WebApplicationFactory<Program> factory) {
        _factory = factory;
    }

    [Fact]
    public async Task GraphQL_Endpoint_Returns_Schema() {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/graphql?sdl");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("type Query", content);
    }

    [Fact]
    public async Task GraphQL_Query_Jobs_Returns_Data() {
        // Arrange
        var client = _factory.CreateClient();
        var request = new {
            query = "{ jobs { id name } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"data\"", content);
    }

    [Fact]
    public async Task GraphQL_Query_Customers_Returns_Data() {
        // Arrange
        var client = _factory.CreateClient();
        var request = new {
            query = "{ customers { customerId name } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"data\"", content);
    }

    [Fact]
    public async Task GraphQL_Query_Vehicles_Returns_Data() {
        // Arrange
        var client = _factory.CreateClient();
        var request = new {
            query = "{ vehicles { id name } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"data\"", content);
    }
}
