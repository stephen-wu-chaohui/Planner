using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Planner.API.Services;

namespace Planner.API.Tests;

public sealed class AzureSignalRConnectionInfoServiceTests {
    [Fact]
    public void CreateConnectionInfo_returns_null_when_access_key_is_not_base64() {
        var config = BuildConfiguration("Endpoint=https://planner-dev.service.signalr.net;AccessKey=replace_me;Version=1.0;");
        var service = new AzureSignalRConnectionInfoService(
            config,
            ProductionEnvironment(),
            NullLogger<AzureSignalRConnectionInfoService>.Instance);

        var result = service.CreateConnectionInfo(Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    [Fact]
    public void CreateConnectionInfo_returns_null_when_access_key_is_too_short_for_signing() {
        var accessKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var config = BuildConfiguration($"Endpoint=https://planner-dev.service.signalr.net;AccessKey={accessKey};Version=1.0;");
        var service = new AzureSignalRConnectionInfoService(
            config,
            ProductionEnvironment(),
            NullLogger<AzureSignalRConnectionInfoService>.Instance);

        var result = service.CreateConnectionInfo(Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    [Fact]
    public void CreateConnectionInfo_returns_connection_info_when_connection_string_is_valid() {
        var accessKey = Convert.ToBase64String(Enumerable.Range(0, 64).Select(i => (byte)i).ToArray());
        var config = BuildConfiguration($"Endpoint=https://planner-dev.service.signalr.net;AccessKey={accessKey};Version=1.0;");
        var service = new AzureSignalRConnectionInfoService(
            config,
            ProductionEnvironment(),
            NullLogger<AzureSignalRConnectionInfoService>.Instance);

        var result = service.CreateConnectionInfo(Guid.NewGuid().ToString());

        result.Should().NotBeNull();
        result!.Url.Should().Be("https://planner-dev.service.signalr.net/client/?hub=planner");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateConnectionInfo_returns_null_outside_production() {
        var accessKey = Convert.ToBase64String(Enumerable.Range(0, 64).Select(i => (byte)i).ToArray());
        var config = BuildConfiguration($"Endpoint=https://planner-dev.service.signalr.net;AccessKey={accessKey};Version=1.0;");
        var service = new AzureSignalRConnectionInfoService(
            config,
            new TestHostEnvironment(Environments.Development),
            NullLogger<AzureSignalRConnectionInfoService>.Instance);

        var result = service.CreateConnectionInfo(Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    private static IConfiguration BuildConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["SignalR:ConnectionString"] = connectionString,
                ["SignalR:HubName"] = "planner"
            })
            .Build();

    private static IHostEnvironment ProductionEnvironment() =>
        new TestHostEnvironment(Environments.Production);

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Planner.API.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
