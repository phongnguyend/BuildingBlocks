﻿using Azure.Identity;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueuesOptions
{
    public string ConnectionString { get; set; }

    public Dictionary<string, string> QueueNames { get; set; }

    public QueueClientOptions QueueClientOptions { get; set; }
}

public class AzureQueueOptions
{
    public string ConnectionString { get; set; }

    public string QueueName { get; set; }

    public QueueClientOptions QueueClientOptions { get; set; }

    public QueueClient CreateQueueClient()
    {
        var options = GetQueueClientOptions();

        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return options == null ?
                new QueueClient(ConnectionString, QueueName) :
                new QueueClient(ConnectionString, QueueName, options);
        }
        else
        {
            var queueUri = new Uri($"https://{QueueName}.queue.core.windows.net/{QueueName}");
            return options == null ?
                new QueueClient(queueUri, new DefaultAzureCredential()) :
                new QueueClient(queueUri, new DefaultAzureCredential(), options);
        }
    }

    private QueueClientOptions GetQueueClientOptions()
    {
        if (QueueClientOptions is null)
        {
            return null;
        }

        return new QueueClientOptions
        {
            MessageEncoding = QueueClientOptions.MessageEncoding
        };
    }
}
