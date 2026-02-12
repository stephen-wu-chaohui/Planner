using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Planner.BlazorApp.Auth;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Simple GraphQL client that uses HttpClient to execute GraphQL queries and mutations
/// </summary>
public sealed class GraphQLClient {
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) {
        PropertyNameCaseInsensitive = true
    };

    static GraphQLClient() {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenStore _tokenStore;

    public GraphQLClient(IHttpClientFactory httpClientFactory, IJwtTokenStore tokenStore) {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
    }

    private HttpClient CreateClient() {
        var client = _httpClientFactory.CreateClient("PlannerApi");

        if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken) && !_tokenStore.IsExpired()) {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
        }
        else {
            client.DefaultRequestHeaders.Authorization = null;
        }

        return client;
    }

    public async Task<GraphQLResponse<T>?> ExecuteAsync<T>(string query, object? variables = null, CancellationToken cancellationToken = default) {
        var request = new GraphQLRequest {
            Query = query,
            Variables = variables
        };

        using var client = CreateClient();
        
        Console.WriteLine($"[GraphQLClient] Sending GraphQL request to {client.BaseAddress}/graphql");
        Console.WriteLine($"[GraphQLClient] Has Authorization header: {client.DefaultRequestHeaders.Authorization != null}");
        
        var response = await client.PostAsJsonAsync("/graphql", request, cancellationToken);

        Console.WriteLine($"[GraphQLClient] Response status: {response.StatusCode}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            _tokenStore.Clear();
            Console.WriteLine("[GraphQLClient] Unauthorized - clearing token");
            return null;
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<GraphQLResponse<T>>(stream, SerializerOptions, cancellationToken);
        
        if (result?.Errors != null && result.Errors.Length > 0)
        {
            Console.WriteLine($"[GraphQLClient] GraphQL errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        }
        
        return result;
    }
}

public class GraphQLRequest {
    public string Query { get; set; } = string.Empty;
    public object? Variables { get; set; }
}

public class GraphQLResponse<T> {
    public T? Data { get; set; }
    public GraphQLError[]? Errors { get; set; }
}

public class GraphQLError {
    public string Message { get; set; } = string.Empty;
    public JsonElement? Extensions { get; set; }
}
