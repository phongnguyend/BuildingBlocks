using Azure.Messaging.EventGrid;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridSender<T> : IMessageSender<T>
{
    private readonly AzureEventGridOptions _options;

    public AzureEventGridSender(AzureEventGridOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        var client = _options.CreateEventGridPublisherClient();

        var data = new BinaryData(new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject());

        var events = new List<EventGridEvent>()
        {
            new EventGridEvent("TEST", typeof(T).FullName, "1.0", data)
            {
                Id = Guid.NewGuid().ToString(),
                EventType = typeof(T).FullName,
                Topic = _options.Topic,
                EventTime = DateTime.UtcNow,
                Subject = "TEST",
                DataVersion = "1.0",
            },
        };

        await client.SendEventsAsync(events, cancellationToken);
    }
}
