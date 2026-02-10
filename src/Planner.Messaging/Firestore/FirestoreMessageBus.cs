using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Planner.Messaging.Firestore;

public interface IFirestoreMessageBus
{
    Task PublishAsync<T>(string topic, T message);
    Task PublishAsync<T>(string topic, string documentId, T message);
    Task<FirestoreChangeListener> SubscribeAsync<T>(string topic, Func<T, Task> onMessage, string documentId);
    Task<FirestoreChangeListener> SubscribeToCollectionAsync<T>(string topic, Func<T, string, Task> onMessage);
}

public sealed class FirestoreMessageBus(IFirestoreConnectionFactory connectionFactory, ILogger<FirestoreMessageBus> logger) : IFirestoreMessageBus
{
    private readonly FirestoreDb? _db = connectionFactory.Create();

    public async Task PublishAsync<T>(string topic, T message)
    {
        if (_db == null)
        {
            logger.LogWarning("Firestore is not configured. Cannot publish message to topic '{Topic}'.", topic);
            return;
        }

        try
        {
            var collectionRef = _db.Collection(topic);
            var data = ToDictionary(message);
            await collectionRef.AddAsync(data);
            logger.LogInformation("Message published to Firestore topic '{Topic}'.", topic);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message to Firestore topic '{Topic}'.", topic);
        }
    }

    public async Task PublishAsync<T>(string topic, string documentId, T message)
    {
        if (_db == null)
        {
            logger.LogWarning("Firestore is not configured. Cannot publish message to topic '{Topic}', document '{DocumentId}'.", topic, documentId);
            return;
        }

        try
        {
            var docRef = _db.Collection(topic).Document(documentId);
            var data = ToDictionary(message);
            await docRef.SetAsync(data);
            logger.LogInformation("Message published to Firestore topic '{Topic}', document '{DocumentId}'.", topic, documentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message to Firestore topic '{Topic}', document '{DocumentId}'.", topic, documentId);
        }
    }

    public async Task<FirestoreChangeListener> SubscribeAsync<T>(string topic, Func<T, Task> onMessage, string documentId)
    {
        if (_db == null)
        {
            logger.LogWarning("Firestore is not configured. Cannot subscribe to topic '{Topic}'.", topic);
            return null;
        }

        var docRef = _db.Collection(topic).Document(documentId);
        var listener = docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                try
                {
                    var message = FromSnapshot<T>(snapshot);
                    if (message != null)
                    {
                        logger.LogInformation("Received message from Firestore topic '{Topic}', document '{DocumentId}'.", topic, documentId);
                        onMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from Firestore topic '{Topic}', document '{DocumentId}'.", topic, documentId);
                }
            }
        });

        logger.LogInformation("Subscribed to Firestore topic '{Topic}', document '{DocumentId}'.", topic, documentId);
        return await Task.FromResult(listener);
    }

    public async Task<FirestoreChangeListener> SubscribeToCollectionAsync<T>(string topic, Func<T, string, Task> onMessage)
    {
        if (_db == null)
        {
            logger.LogWarning("Firestore is not configured. Cannot subscribe to collection '{Topic}'.", topic);
            return null;
        }

        var collectionRef = _db.Collection(topic);
        var listener = collectionRef.Listen(snapshot =>
        {
            foreach (var change in snapshot.Changes)
            {
                if (change.ChangeType == DocumentChange.Type.Added)
                {
                    try
                    {
                        var message = FromSnapshot<T>(change.Document);
                        if (message != null)
                        {
                            logger.LogInformation("Received new message from Firestore collection '{Topic}', document '{DocumentId}'.", topic, change.Document.Id);
                            onMessage(message, change.Document.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing collection message from Firestore collection '{Topic}', document '{DocumentId}'.", topic, change.Document.Id);
                    }
                }
            }
        });

        logger.LogInformation("Subscribed to Firestore collection '{Topic}'.", topic);
        return await Task.FromResult(listener);
    }

    private static Dictionary<string, object> ToDictionary<T>(T message)
    {
        if (message is Dictionary<string, object> dict) return dict;

        var json = JsonConvert.SerializeObject(message);
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? [];
    }

    private static T? FromSnapshot<T>(DocumentSnapshot snapshot)
    {
        if (typeof(T) == typeof(Dictionary<string, object>))
        {
            return (T)(object)snapshot.ToDictionary();
        }

        var data = snapshot.ToDictionary();
        var json = JsonConvert.SerializeObject(data);
        return JsonConvert.DeserializeObject<T>(json);
    }
}