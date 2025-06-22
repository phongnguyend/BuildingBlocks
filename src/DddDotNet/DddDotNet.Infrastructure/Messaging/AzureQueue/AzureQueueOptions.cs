using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueueOptions
{
    public string ConnectionString { get; set; }

    public Dictionary<string, string> QueueNames { get; set; }
}
