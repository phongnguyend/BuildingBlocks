using Azure.Identity;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueuesOptions
{
    public string ConnectionString { get; set; }

    public Dictionary<string, string> QueueNames { get; set; }

    public QueueMessageEncoding MessageEncoding { get; set; }
}

public class AzureQueueOptions
{
    public string ConnectionString { get; set; }

    public string QueueName { get; set; }

    public QueueMessageEncoding MessageEncoding { get; set; } = QueueMessageEncoding.None;

    public QueueClient CreateQueueClient()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new QueueClient(ConnectionString, QueueName, new QueueClientOptions { MessageEncoding = MessageEncoding });
        }
        else
        {
            var queueUri = new Uri($"https://{QueueName}.queue.core.windows.net/{QueueName}");
            return new QueueClient(queueUri, new DefaultAzureCredential(), new QueueClientOptions { MessageEncoding = MessageEncoding });
        }
    }
}
