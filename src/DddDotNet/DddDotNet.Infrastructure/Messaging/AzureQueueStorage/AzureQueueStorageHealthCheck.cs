using DddDotNet.Infrastructure.HealthChecks;
using System;

namespace DddDotNet.Infrastructure.Messaging.AzureQueueStorage;

public class AzureQueueStorageHealthCheck : TcpHealthCheck
{
    public AzureQueueStorageHealthCheck(string queueUrl)
        : base(new Uri(queueUrl).Host, [443])
    {
    }
}
