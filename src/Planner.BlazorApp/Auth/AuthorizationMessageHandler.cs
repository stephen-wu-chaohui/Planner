using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Planner.BlazorApp.Auth;

/// <summary>
/// Delegating handler that attaches a Bearer token to outgoing API requests using
/// the MSAL-based <see cref="IAccessTokenProvider"/> provided by Blazor WebAssembly.
/// </summary>
public class AuthorizationMessageHandler(
    IAccessTokenProvider tokenProvider,
    NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tokenResult = await tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out var token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Value);
        }
        else
        {
            // Token not available – redirect to MSAL login and abort the request.
            navigationManager.NavigateToLogin("authentication/login");
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
