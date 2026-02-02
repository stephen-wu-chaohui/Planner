using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Planner.Messaging.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.API.EndToEndTests.Fixtures;

public sealed class FakeMessageBus : IMessageBus {
    public List<(string Route, object Message)> PublishedMessages { get; } = [];

    public Task PublishAsync<T>(string queueName, T message) {
        PublishedMessages.Add((queueName, message));
        return Task.CompletedTask;
    }

    IDisposable IMessageBus.Subscribe<T>(string queueName, Func<T, Task> onMessage) {
        return null;
    }
}
