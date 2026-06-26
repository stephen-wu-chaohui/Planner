using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Planner.BlazorApp.Auth;

public sealed class PlannerApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public PlannerApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is missing in appsettings.");
        var apiScope = configuration["Api:Scope"]
            ?? throw new InvalidOperationException("Api:Scope is missing in appsettings.");

        ConfigureHandler(
            authorizedUrls: [apiBaseUrl.TrimEnd('/')],
            scopes: [apiScope]);
    }
}
