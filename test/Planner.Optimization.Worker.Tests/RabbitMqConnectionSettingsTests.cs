using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Planner.Messaging.RabbitMQ;
using RabbitMQ.Client;

namespace Planner.Optimization.Worker.Tests;

public sealed class RabbitMqConnectionSettingsTests {
    [Fact]
    public void FromConfiguration_UsesPlainHostSettings() {
        var settings = RabbitMqConnectionSettings.FromConfiguration(Configuration([
            Setting("RabbitMq:Host", "rabbitmq"),
            Setting("RabbitMq:User", "planner"),
            Setting("RabbitMq:Pass", "planner-pass"),
            Setting("RabbitMq:Port", "5673")
        ]));

        var factory = new ConnectionFactory();
        settings.ApplyTo(factory);

        factory.HostName.Should().Be("rabbitmq");
        factory.UserName.Should().Be("planner");
        factory.Password.Should().Be("planner-pass");
        factory.Port.Should().Be(5673);
        factory.DispatchConsumersAsync.Should().BeTrue();
        settings.EndpointForLog.Should().Be("rabbitmq:5673");
    }

    [Fact]
    public void FromConfiguration_UsesUriSettingsWithoutLoggingCredentials() {
        var uriWithCredentials = new UriBuilder("amqps", "fly.rmq.cloudamqp.com") {
            UserName = "user-name",
            Password = "uri-pass",
            Path = "vhost-name"
        }.Uri.ToString();

        var settings = RabbitMqConnectionSettings.FromConfiguration(Configuration([
            Setting("RabbitMq:Host", uriWithCredentials),
            Setting("RabbitMq:Port", "5672")
        ]));

        var factory = new ConnectionFactory();
        settings.ApplyTo(factory);

        factory.HostName.Should().Be("fly.rmq.cloudamqp.com");
        factory.UserName.Should().Be("user-name");
        factory.Password.Should().Be("uri-pass");
        factory.VirtualHost.Should().Be("vhost-name");
        factory.Port.Should().Be(5671);
        factory.Ssl.Enabled.Should().BeTrue();
        factory.DispatchConsumersAsync.Should().BeTrue();
        settings.EndpointForLog.Should().Be("amqps://fly.rmq.cloudamqp.com:5671/vhost-name");
        settings.EndpointForLog.Should().NotContain("user-name");
        settings.EndpointForLog.Should().NotContain("uri-pass");
    }

    [Fact]
    public void FromConfiguration_AppliesSeparateCredentialsToUriWithoutUserInfo() {
        var settings = RabbitMqConnectionSettings.FromConfiguration(Configuration([
            Setting("RabbitMq:Host", "amqp://broker.example.com/my-vhost"),
            Setting("RabbitMq:User", "configured-user"),
            Setting("RabbitMq:Pass", "configured-password")
        ]));

        var factory = new ConnectionFactory();
        settings.ApplyTo(factory);

        factory.HostName.Should().Be("broker.example.com");
        factory.UserName.Should().Be("configured-user");
        factory.Password.Should().Be("configured-password");
        factory.VirtualHost.Should().Be("my-vhost");
        settings.EndpointForLog.Should().Be("amqp://broker.example.com:5672/my-vhost");
    }

    [Fact]
    public void FromConfiguration_RejectsInvalidUriStyleHost() {
        var act = () => RabbitMqConnectionSettings.FromConfiguration(Configuration([
            Setting("RabbitMq:Host", "https://broker.example.com")
        ]));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("RabbitMq connection configuration must be a host name or a valid amqp:// or amqps:// URI.");
    }

    private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static KeyValuePair<string, string?> Setting(string key, string value) =>
        new(key, value);
}
