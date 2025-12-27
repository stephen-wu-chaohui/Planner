using Microsoft.Extensions.Logging;
using Planner.Contracts.Optimization.Abstractions;
using Planner.Contracts.Optimization.Requests;
using Planner.Messaging;

namespace Planner.Optimization.Worker.Handlers;

public sealed class OptimizationRequestHandler(
    ILogger<OptimizationRequestHandler> logger,
    IRouteOptimizer optimizer,
    IMessageBus bus) : IOptimizationRequestHandler {

    public async Task HandleAsync(
        OptimizeRouteRequest request,
        CancellationToken ct) {
        logger.LogInformation(
            "Handling optimization run {RunId} for tenant {TenantId}",
            request.OptimizationRunId,
            request.TenantId);

        Validate(request);

        var response = optimizer.Optimize(request);

        await bus.PublishAsync(
            MessageRoutes.Response,
            response);
    }

    private static void Validate(OptimizeRouteRequest request) {
        if (request.OptimizationRunId == Guid.Empty)
            throw new InvalidOperationException("OptimizationRunId missing.");

        if (request.Vehicles.Count == 0)
            throw new InvalidOperationException("No vehicles provided.");

        if (request.Jobs.Count == 0)
            throw new InvalidOperationException("No jobs provided.");
    }
}
