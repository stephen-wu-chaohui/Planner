using System.Net.Http.Json;
using System.Net;

namespace Planner.BlazorApp.Services;

public sealed class PlannerApiClient(
    IHttpClientFactory httpClientFactory)
{
    private HttpClient CreateClient()
    {
        return httpClientFactory.CreateClient("PlannerApi");
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

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NoContent)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        return await CreateClient().PutAsJsonAsync(requestUri, value, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, long id, CancellationToken cancellationToken = default) {
        requestUri = $"{requestUri}/{id}";
        var response = await CreateClient().DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            return response;
        }

        response.EnsureSuccessStatusCode();
        return response;
    }
}
