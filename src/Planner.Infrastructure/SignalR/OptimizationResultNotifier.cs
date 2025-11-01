using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Infrastructure.SignalR;

public class OptimizationResultNotifier(IHubContext<PlannerHub> hubContext) : IMessageHubPublisher
{
    public Task PublishAsync<T>(string method, T message) where T : class
    {
        // Broadcast to all connected SignalR clients
        hubContext.Clients.All.SendAsync(method, message);
        Console.WriteLine($"Broadcast {method}, {message}");
        return Task.CompletedTask;
    }
}
