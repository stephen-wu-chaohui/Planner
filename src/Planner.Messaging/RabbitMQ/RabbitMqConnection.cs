using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Planner.Messaging.RabbitMQ;

public interface IRabbitMqConnection : IDisposable {
    bool IsConnected { get; }
    IModel CreateChannel();
}

internal sealed class RabbitMqConnection : IRabbitMqConnection {
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly object _lock = new();
    private readonly ILogger<RabbitMqConnection> _logger;

    public RabbitMqConnection(
        IConfiguration configuration,
        ILogger<RabbitMqConnection> logger) {
        _logger = logger;

        _factory = new ConnectionFactory {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            UserName = configuration["RabbitMq:User"] ?? "guest",
            Password = configuration["RabbitMq:Pass"] ?? "guest",
            Port = int.TryParse(configuration["RabbitMq:Port"], out var p) ? p : 5672,
            DispatchConsumersAsync = true
        };

        _logger.LogInformation(
            "RabbitMQ configured for host {Host}:{Port}",
            _factory.HostName,
            _factory.Port
        );
    }

    public bool IsConnected => _connection?.IsOpen == true;

    public IModel CreateChannel() {
        EnsureConnected();

        return _connection!.CreateModel();
    }

    private void EnsureConnected() {
        if (IsConnected) return;

        lock (_lock) {
            if (IsConnected) return;

            try {
                _connection = _factory.CreateConnection();
                RegisterEventHandlers(_connection);

                _logger.LogInformation(
                    "RabbitMQ connection established to {Host}",
                    _connection.Endpoint.HostName
                );
            } catch (Exception ex) {
                _logger.LogError(ex, "RabbitMQ connection failed");
                throw; // fail fast, caller decides retry
            }
        }
    }

    private void RegisterEventHandlers(IConnection connection) {
        connection.ConnectionShutdown += (_, e) =>
            _logger.LogWarning("RabbitMQ shutdown: {Reason}", e.ReplyText);

        connection.CallbackException += (_, e) =>
            _logger.LogError(e.Exception, "RabbitMQ callback exception");

        connection.ConnectionBlocked += (_, e) =>
            _logger.LogWarning("RabbitMQ blocked: {Reason}", e.Reason);
    }

    public void Dispose() {
        try {
            _connection?.Close();
            _connection?.Dispose();
        } catch (Exception ex) {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
