using DddDotNet.Infrastructure.Messaging.AzureEventGrid;
using DddDotNet.Infrastructure.Messaging.AzureEventHub;
using DddDotNet.Infrastructure.Messaging.AzureQueueStorage;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using DddDotNet.Infrastructure.Messaging.Kafka;
using DddDotNet.Infrastructure.Messaging.RabbitMQ;

namespace DddDotNet.Infrastructure.Messaging;

public class MessagingOptions
{
    public string Provider { get; set; }

    public RabbitMQOptions RabbitMQ { get; set; }

    public KafkaOptions Kafka { get; set; }

    public AzureQueuesOptions AzureQueue { get; set; }

    public AzureServiceBusOptions AzureServiceBus { get; set; }

    public AzureEventGridsOptions AzureEventGrid { get; set; }

    public AzureEventHubsOptions AzureEventHub { get; set; }

    public bool UsedRabbitMQ()
    {
        return Provider == "RabbitMQ";
    }

    public bool UsedKafka()
    {
        return Provider == "Kafka";
    }

    public bool UsedAzureQueue()
    {
        return Provider == "AzureQueue";
    }

    public bool UsedAzureServiceBus()
    {
        return Provider == "AzureServiceBus";
    }

    public bool UsedAzureEventGrid()
    {
        return Provider == "AzureEventGrid";
    }

    public bool UsedAzureEventHub()
    {
        return Provider == "AzureEventHub";
    }

    public bool UsedFake()
    {
        return Provider == "Fake";
    }
}
