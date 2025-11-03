using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Planner.Application.Messaging;

public class OptimizationResultReceiver(IConfiguration configuration) : IMessageHubClient {
    public Task SubscribeAsync<T>(string method, Action<T> handler) where T : class {
        var hubUrl = configuration["SignalR:Server"]! + configuration["SignalR:Route"]!;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<T>(method, handler);

        Console.WriteLine($"SubscribeAsync<T>({method}, Action<T> handler)");

        return hubConnection.StartAsync();
    }
}
