using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Planner.Messaging.RabbitMQ;

internal sealed class RabbitMqConnectionSettings {
    private const int DefaultPort = 5672;
    private const int DefaultSslPort = 5671;
    private readonly Uri? _uri;
    private readonly string? _hostName;
    private readonly string? _userName;
    private readonly string? _password;
    private readonly int _port;

    private RabbitMqConnectionSettings(Uri uri) {
        _uri = uri;
        EndpointForLog = FormatUriForLog(uri);
    }

    private RabbitMqConnectionSettings(string hostName, string userName, string password, int port) {
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _port = port;
        EndpointForLog = $"{hostName}:{port}";
    }

    public string EndpointForLog { get; }

    public static RabbitMqConnectionSettings FromConfiguration(IConfiguration configuration) {
        var configuredUri = FirstNonWhiteSpace(
            configuration["RabbitMq:Uri"],
            configuration["RabbitMq:ConnectionString"]);
        var configuredHost = configuration["RabbitMq:Host"];
        var endpointValue = FirstNonWhiteSpace(configuredUri, configuredHost) ?? "localhost";

        if (TryCreateAmqpUri(endpointValue, out var uri)) {
            return new RabbitMqConnectionSettings(ApplyCredentialOverrides(uri, configuration));
        }

        if (LooksLikeUri(endpointValue)) {
            throw new InvalidOperationException(
                "RabbitMq connection configuration must be a host name or a valid amqp:// or amqps:// URI.");
        }

        var userName = FirstNonWhiteSpace(configuration["RabbitMq:User"]) ?? "guest";
        var password = FirstNonWhiteSpace(configuration["RabbitMq:Pass"]) ?? "guest";
        var port = GetConfiguredPort(configuration, DefaultPort);

        return new RabbitMqConnectionSettings(endpointValue, userName, password, port);
    }

    public void ApplyTo(ConnectionFactory factory) {
        if (_uri is not null) {
            factory.Uri = _uri;
        } else {
            factory.HostName = _hostName!;
            factory.UserName = _userName!;
            factory.Password = _password!;
            factory.Port = _port;
        }

        factory.DispatchConsumersAsync = true;
    }

    private static Uri ApplyCredentialOverrides(Uri uri, IConfiguration configuration) {
        if (!string.IsNullOrEmpty(uri.UserInfo)) {
            return uri;
        }

        var configuredUserName = FirstNonWhiteSpace(configuration["RabbitMq:User"]);
        var configuredPassword = FirstNonWhiteSpace(configuration["RabbitMq:Pass"]);
        if (configuredUserName is null && configuredPassword is null) {
            return uri;
        }

        var builder = new UriBuilder(uri);
        if (configuredUserName is not null) {
            builder.UserName = configuredUserName;
        }

        if (configuredPassword is not null) {
            builder.Password = configuredPassword;
        }

        return builder.Uri;
    }

    private static bool TryCreateAmqpUri(string value, out Uri uri) {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri!)
            && (string.Equals(uri.Scheme, "amqp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "amqps", StringComparison.OrdinalIgnoreCase))) {
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool LooksLikeUri(string value) =>
        value.Contains("://", StringComparison.Ordinal);

    private static int GetConfiguredPort(IConfiguration configuration, int defaultPort) =>
        int.TryParse(configuration["RabbitMq:Port"], out var port) && port > 0
            ? port
            : defaultPort;

    private static string FormatUriForLog(Uri uri) {
        var port = uri.Port > 0
            ? uri.Port
            : string.Equals(uri.Scheme, "amqps", StringComparison.OrdinalIgnoreCase)
                ? DefaultSslPort
                : DefaultPort;
        var path = string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal)
            ? string.Empty
            : uri.AbsolutePath;

        return $"{uri.Scheme}://{uri.Host}:{port}{path}";
    }

    private static string? FirstNonWhiteSpace(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
