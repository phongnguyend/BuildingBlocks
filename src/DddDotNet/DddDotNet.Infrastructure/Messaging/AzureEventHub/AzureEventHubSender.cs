using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubSender<T> : IMessageSender<T>
{
    private readonly string _connectionString;
    private readonly string _hubName;

    public AzureEventHubSender(string connectionString, string hubName)
    {
        _connectionString = connectionString;
        _hubName = hubName;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        var producer = new EventHubProducerClient(_connectionString, _hubName);

        var events = new List<EventData>
        {
            new EventData(new Message<T>
            {
                Data = message,
                MetaData = metaData,
            }.SerializeObject()),
        };

        await producer.SendAsync(events, cancellationToken);

        await producer.CloseAsync(cancellationToken);
    }
}
