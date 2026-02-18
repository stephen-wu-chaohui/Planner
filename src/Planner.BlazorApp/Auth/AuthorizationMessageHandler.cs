using System.Net.Http.Headers;
using Microsoft.Identity.Web;

namespace Planner.BlazorApp.Auth;

public class AuthorizationMessageHandler(ITokenAcquisition tokenAcquisition, IConfiguration configuration) : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        // Define the scope your API requires. 
        // Usually something like "api://<your-api-client-id>/access_as_user"
        var scope = configuration["Api:Scope"]
            ?? throw new InvalidOperationException("Api:Scope is missing in appsettings.");

        try {
            // This pulls from the cache if valid, or securely fetches a new one using the ClientSecret
            var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync([scope]);

            // Attach the Bearer token to the outgoing HTTP request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        } catch (MicrosoftIdentityWebChallengeUserException ex) {
            // If the user's session is totally invalid, this forces them to log in again
            throw new UnauthorizedAccessException("User needs to re-authenticate.", ex);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
