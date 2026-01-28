using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubsOptions
{
    public string ConnectionString { get; set; }

    public string StorageConnectionString { get; set; }

    public Dictionary<string, string> Hubs { get; set; }

    public Dictionary<string, string> StorageContainerNames { get; set; }
}

public class AzureEventHubOptions
{
    public bool UseManagedIdentity { get; set; }

    public string ConnectionString { get; set; }

    public string HubName { get; set; }

    public string StorageConnectionString { get; set; }

    public string StorageContainerName { get; set; }

    public EventHubProducerClient CreateEventHubProducerClient()
    {
        if (UseManagedIdentity)
        {
            var eventHubNamespace = HubName.Split('/')[0];
            var eventHubUri = $"sb://{eventHubNamespace}.servicebus.windows.net/";
            return new EventHubProducerClient(eventHubUri, HubName, new DefaultAzureCredential());
        }

        return new EventHubProducerClient(ConnectionString, HubName);
    }

    private BlobContainerClient CreateBlobContainerClient()
    {
        if (UseManagedIdentity)
        {
            var containerUri = new Uri($"https://{StorageContainerName}.blob.core.windows.net");
            return new BlobContainerClient(containerUri, new DefaultAzureCredential());
        }

        return new BlobContainerClient(StorageConnectionString, StorageContainerName);
    }

    public EventProcessorClient CreateEventProcessorClient(string consumerGroup)
    {
        var storageClient = CreateBlobContainerClient();

        if (UseManagedIdentity)
        {
            var eventHubsNamespace = HubName.Split('/')[0];
            var fullyQualifiedNamespace = $"{eventHubsNamespace}.servicebus.windows.net";
            return new EventProcessorClient(
                storageClient,
                consumerGroup,
                fullyQualifiedNamespace,
                HubName,
                new DefaultAzureCredential());
        }

        return new EventProcessorClient(
            storageClient,
            consumerGroup,
            ConnectionString,
            HubName);
    }
}
