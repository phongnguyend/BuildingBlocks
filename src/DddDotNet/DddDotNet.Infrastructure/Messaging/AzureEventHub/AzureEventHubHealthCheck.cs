using DddDotNet.Infrastructure.HealthChecks;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubHealthCheck : TcpHealthCheck
{
    public AzureEventHubHealthCheck(string @namespace, IReadOnlyCollection<int> ports)
        : base(@namespace, ports)
    {
    }
}
