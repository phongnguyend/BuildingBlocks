using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusOptions
{
    public string ConnectionString { get; set; }

    public Dictionary<string, string> QueueNames { get; set; }
}
