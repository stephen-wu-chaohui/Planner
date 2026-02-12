using System.Net.Http.Json;
using System.Text.Json;
using Planner.BlazorApp.Auth;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Simple GraphQL client that uses HttpClient to execute GraphQL queries and mutations
/// </summary>
public sealed class GraphQLClient {
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
        var response = await client.PostAsJsonAsync("/graphql", request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            _tokenStore.Clear();
            return null;
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<T>>(cancellationToken: cancellationToken);
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
    public JsonElement[]? Extensions { get; set; }
}
