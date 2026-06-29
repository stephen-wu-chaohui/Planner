using Microsoft.Extensions.Configuration;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.API.EndToEndTests.Fixtures;

public sealed class FakeMessageBus(
    IConfiguration configuration,
    IOptimizationJobQueue jobQueue) : IMessageBus {

    public List<(string Route, object Message)> PublishedMessages { get; } = [];

    public async Task PublishAsync<T>(string queueName, T message) {
        PublishedMessages.Add((queueName, message!));

        if (UseAzureOptimizationDispatch()
            && queueName == MessageRoutes.Request
            && message is OptimizeRouteRequest request) {
            await jobQueue.EnqueueAsync(
                new OptimizationJobMessage(request.TenantId, request.OptimizationRunId),
                CancellationToken.None);
        }
    }

    IDisposable IMessageBus.Subscribe<T>(string queueName, Func<T, Task> onMessage) =>
        new Disposable();

    private bool UseAzureOptimizationDispatch() =>
        string.Equals(
            configuration["Optimization:DispatchMode"],
            "AzureServiceBus",
            StringComparison.OrdinalIgnoreCase)
        || string.Equals(
            configuration["OptimizationMessaging:Transport"],
            "ServiceBus",
            StringComparison.OrdinalIgnoreCase);

    private sealed class Disposable : IDisposable {
        public void Dispose() {
        }
    }
}
