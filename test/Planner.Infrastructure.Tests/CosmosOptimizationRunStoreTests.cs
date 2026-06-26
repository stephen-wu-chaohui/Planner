using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Planner.Infrastructure.OptimizationRuns;

namespace Planner.Infrastructure.Tests;

public sealed class CosmosOptimizationRunStoreTests {
    [Fact]
    public void CreateClientOptions_uses_configured_connection_mode_and_timeout() {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cosmos:ConnectionMode"] = "Gateway",
                ["Cosmos:RequestTimeoutSeconds"] = "15"
            })
            .Build();

        var options = CreateClientOptions(config);

        options.ConnectionMode.Should().Be(ConnectionMode.Gateway);
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void CreateClientOptions_ignores_invalid_timeout_values() {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cosmos:RequestTimeoutSeconds"] = "0"
            })
            .Build();

        var options = CreateClientOptions(config);

        options.RequestTimeout.Should().Be(new CosmosClientOptions().RequestTimeout);
    }

    private static CosmosClientOptions CreateClientOptions(IConfiguration configuration) {
        var method = typeof(CosmosOptimizationRunStore).GetMethod(
            "CreateClientOptions",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return (CosmosClientOptions)method!.Invoke(null, [configuration])!;
    }
}
