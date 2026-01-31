using Planner.BlazorApp.Auth;
using System.Net.Http.Headers;

namespace Planner.BlazorApp.Services;

public sealed class PlannerApiClient(
    IHttpClientFactory httpClientFactory,
    IJwtTokenStore tokenStore)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("PlannerApi");

        // With cookie-based authentication, we no longer need to manually set Authorization headers
        // The HttpClient will automatically include cookies in requests
        // Keep token store for backward compatibility and token inspection
        
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
            tokenStore?.Clear();
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync(requestUri, value, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            tokenStore?.Clear();
        }
        return response;
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, long id, CancellationToken cancellationToken = default) {
        requestUri = $"{requestUri}/{id}";
        var response = await CreateClient().DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            tokenStore.Clear();
            return response;
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

}