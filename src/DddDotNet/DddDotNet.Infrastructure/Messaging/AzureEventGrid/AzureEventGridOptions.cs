using Azure;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using System;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridsOptions
{
    public string DomainEndpoint { get; set; }

    public string DomainKey { get; set; }

    public Dictionary<string, string> Topics { get; set; }
}

public class AzureEventGridOptions
{
    public bool UseManagedIdentity { get; set; }

    public string DomainEndpoint { get; set; }

    public string DomainKey { get; set; }

    public string Topic { get; set; }

    public EventGridPublisherClient CreateEventGridPublisherClient()
    {
        if (UseManagedIdentity)
        {
            return new EventGridPublisherClient(new Uri(DomainEndpoint), new DefaultAzureCredential());
        }

        return new EventGridPublisherClient(new Uri(DomainEndpoint), new AzureKeyCredential(DomainKey));
    }
}
