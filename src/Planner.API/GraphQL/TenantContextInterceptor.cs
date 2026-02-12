using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Planner.Application;
using System.Security;

namespace Planner.API.GraphQL;

/// <summary>
/// GraphQL request interceptor that extracts tenant context from middleware and stores it in GraphQL context.
/// This bridges the gap between ASP.NET Core DI scope and HotChocolate's resolver DI scope.
/// </summary>
public sealed class TenantContextInterceptor : DefaultHttpRequestInterceptor
{
    private const string TenantIdKey = "TenantId";

    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<TenantContextInterceptor>>();

        try
        {
            // Get tenant context from HTTP request scope (set by TenantContextMiddleware)
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            
            logger.LogInformation(
                "[GraphQL Interceptor] HTTP Scope - TenantContext Instance: {Hash}, IsSet: {IsSet}", 
                tenantContext.GetHashCode(), 
                tenantContext.IsSet);

            // Validate authentication
            if (context.User.Identity?.IsAuthenticated != true)
            {
                logger.LogWarning("[GraphQL Interceptor] Unauthenticated GraphQL request");
                throw new SecurityException("GraphQL requests require authentication.");
            }

            // Validate tenant context was set by middleware
            if (!tenantContext.IsSet)
            {
                logger.LogError(
                    "[GraphQL Interceptor] Authenticated user {User} but tenant context is NOT set!",
                    context.User.Identity.Name ?? "Unknown");
                throw new SecurityException("Tenant context is not set for authenticated user.");
            }

            // Store TenantId in GraphQL context properties so resolvers can access it
            // This bridges the gap between HTTP request scope and GraphQL resolver scope
            var tenantId = tenantContext.TenantId;
            requestBuilder.SetGlobalState(TenantIdKey, tenantId);

            logger.LogInformation(
                "[GraphQL Interceptor] Stored TenantId {TenantId} in GraphQL context for user {User}",
                tenantId,
                context.User.Identity.Name ?? "Unknown");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GraphQL Interceptor] Error in OnCreateAsync");
            throw;
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
