using Azure.Messaging.EventHubs;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubSender<T> : IMessageSender<T>
{
    private readonly AzureEventHubOptions _options;

    public AzureEventHubSender(AzureEventHubOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        var producer = _options.CreateEventHubProducerClient();

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
