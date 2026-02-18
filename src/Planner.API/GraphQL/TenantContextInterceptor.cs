using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Planner.Application;

namespace Planner.API.GraphQL;

/// <summary>
/// GraphQL request interceptor that extracts tenant context from middleware and stores it in GraphQL context.
/// This bridges the gap between ASP.NET Core DI scope and HotChocolate's resolver DI scope.
/// </summary>
public class TenantContextInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder, CancellationToken cancellationToken)
    {
        // ITenantContext was already populated by your Middleware
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();

        if (tenantContext.IsSet) {
            // Push it into the GraphQL global state
            requestBuilder.SetGlobalState("TenantId", tenantContext.TenantId);
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
