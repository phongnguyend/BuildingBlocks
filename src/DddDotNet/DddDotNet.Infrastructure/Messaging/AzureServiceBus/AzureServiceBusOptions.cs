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
    public bool UseManagedIdentity { get; set; }

    public string ConnectionString { get; set; }

    public string Namespace { get; set; }

    public ServiceBusClientOptions ServiceBusClientOptions { get; set; }

    public ServiceBusClient CreateServiceBusClient()
    {
        var options = GetServiceBusClientOptions();

        if (UseManagedIdentity)
        {
            return options == null ?
                new ServiceBusClient(Namespace, new DefaultAzureCredential()) :
                new ServiceBusClient(Namespace, new DefaultAzureCredential(), options);
        }

        return options == null ?
            new ServiceBusClient(ConnectionString) :
            new ServiceBusClient(ConnectionString, options);
    }

    private ServiceBusClientOptions GetServiceBusClientOptions()
    {
        if (ServiceBusClientOptions is null)
        {
            return null;
        }

        return new ServiceBusClientOptions
        {
            TransportType = ServiceBusClientOptions.TransportType
        };
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
