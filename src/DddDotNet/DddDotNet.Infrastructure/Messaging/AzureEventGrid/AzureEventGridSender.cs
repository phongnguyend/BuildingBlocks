﻿using Azure;
using Azure.Messaging.EventGrid;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridSender<T> : IMessageSender<T>
{
    private readonly string _domainEndpoint;
    private readonly string _domainKey;
    private readonly string _topic;

    public AzureEventGridSender(string domainEndpoint, string domainKey, string topic)
    {
        _domainEndpoint = domainEndpoint;
        _domainKey = domainKey;
        _topic = topic;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        EventGridPublisherClient client = new EventGridPublisherClient(new Uri(_domainEndpoint), new AzureKeyCredential(_domainKey));

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
                Topic = _topic,
                EventTime = DateTime.UtcNow,
                Subject = "TEST",
                DataVersion = "1.0",
            },
        };

        await client.SendEventsAsync(events, cancellationToken);
    }
}
