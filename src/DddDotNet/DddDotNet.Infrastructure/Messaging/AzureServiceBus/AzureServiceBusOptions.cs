using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusOptions
{
    public string ConnectionString { get; set; }

    public string Namespace { get; set; }

    public Dictionary<string, string> QueueNames { get; set; }
}

public class AzureServiceBusNameSpaceOptions
{
    public string ConnectionString { get; set; }

    public string Namespace { get; set; }

    public ServiceBusClient CreateServiceBusClient()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new ServiceBusClient(ConnectionString);
        }
        else
        {
            return new ServiceBusClient(Namespace, new DefaultAzureCredential());
        }
    }
}

public class AzureServiceBusQueueOptions : AzureServiceBusNameSpaceOptions
{
    public string QueueName { get; set; }
}

public class AzureServiceBusTopicOptions : AzureServiceBusNameSpaceOptions
{
    public string Topic { get; set; }
}

public class AzureServiceBusSubscriptionOptions : AzureServiceBusNameSpaceOptions
{
    public string Topic { get; set; }

    public string Subscription { get; set; }
}
