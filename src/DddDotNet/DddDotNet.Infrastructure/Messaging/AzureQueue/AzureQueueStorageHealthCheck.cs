using DddDotNet.Infrastructure.HealthChecks;
using System;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueueStorageHealthCheck : TcpHealthCheck
{
    public AzureQueueStorageHealthCheck(string queueUrl)
        : base(new Uri(queueUrl).Host, [443])
    {
    }
}
