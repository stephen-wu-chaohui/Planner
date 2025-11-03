using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Planner.Messaging.RabbitMQ;

public interface IRabbitMqConnection : IDisposable {
    bool IsConnected { get; }
    IModel CreateChannel();
}

public class RabbitMqConnection : IRabbitMqConnection {
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly object _lock;
    private readonly ILogger _logger;


    public RabbitMqConnection(IConfiguration configuration, ILogger<RabbitMqConnection> logger) {
        _factory = new ConnectionFactory {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            Port = int.TryParse(configuration["RabbitMq:Port"], out var port) ? port : 5672,
            DispatchConsumersAsync = true
        };
        _lock = new object();
        _logger = logger;
        TryConnect();
    }

    public bool IsConnected => _connection != null && _connection.IsOpen;

    private void TryConnect() {
        lock (_lock) {
            if (IsConnected) return;

            const int maxRetries = 10;
            for (int i = 0; i < maxRetries; i++) {
                try {
                    _connection = _factory.CreateConnection();
                    RegisterEventHandlers(_connection);
                    _logger.LogInformation("✅ RabbitMQ connection established to {HostName}", _connection.Endpoint.HostName);
                    return;
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Retry {Attempt}/{Max}", i + 1, maxRetries);
                    Thread.Sleep(TimeSpan.FromSeconds(Math.Min(5 * (i + 1), 30))); // backoff
                }
            }

            _logger.LogError("❌ RabbitMQ connection could not be established after {MaxRetries} retries.", maxRetries);
        }
    }

    private void RegisterEventHandlers(IConnection conn) {
        conn.ConnectionShutdown += (_, ea) => {
            _logger.LogWarning("RabbitMQ connection shutdown detected: {Reason}", ea.ReplyText);
            TryReconnect();
        };

        conn.CallbackException += (_, ea) => {
            _logger.LogError(ea.Exception, "RabbitMQ callback exception occurred");
            TryReconnect();
        };

        conn.ConnectionBlocked += (_, ea) => {
            _logger.LogWarning("RabbitMQ connection blocked: {Reason}", ea.Reason);
            TryReconnect();
        };
    }

    private void TryReconnect() {
        if (IsConnected) return;

        _logger.LogWarning("Attempting RabbitMQ reconnection...");
        DisposeConnection();

        // Retry loop
        const int maxAttempts = 20;
        for (int attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                _connection = _factory.CreateConnection();
                RegisterEventHandlers(_connection);
                _logger.LogInformation("✅ Reconnected to RabbitMQ after {Attempts} attempts", attempt);
                return;
            } catch {
                Thread.Sleep(TimeSpan.FromSeconds(Math.Min(3 * attempt, 30)));
            }
        }

        _logger.LogError("❌ RabbitMQ reconnection failed after {MaxAttempts} attempts", maxAttempts);
    }

    public IModel CreateChannel() {
        if (!IsConnected)
            TryReconnect();

        if (_connection == null)
            throw new InvalidOperationException("RabbitMQ connection is not available.");

        return _connection.CreateModel();
    }

    public void Dispose() {
        DisposeConnection();
        GC.SuppressFinalize(this);
    }

    private void DisposeConnection() {
        try {
            _connection?.Close();
            _connection?.Dispose();
        } catch (Exception ex) {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }

        _connection = null;
    }
}
