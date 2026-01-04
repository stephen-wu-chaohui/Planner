using System.Net.Http.Headers;
using System.Net.Http.Json;
using Planner.BlazorApp.Auth;

namespace Planner.BlazorApp.Services;

public sealed class PlannerApiClient(
    IHttpClientFactory httpClientFactory,
    IJwtTokenStore tokenStore)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("PlannerApi");

        // Always set/clear auth per request based on the *current* scoped TokenStore
        if (!string.IsNullOrWhiteSpace(tokenStore.AccessToken) && !tokenStore.IsExpired())
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenStore.AccessToken);
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        return client;
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) =>
        CreateClient().GetAsync(requestUri, cancellationToken);

    public Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default)
        => CreateClient().PostAsJsonAsync(requestUri, value, cancellationToken);

    public async Task<T?> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            tokenStore.Clear();
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}