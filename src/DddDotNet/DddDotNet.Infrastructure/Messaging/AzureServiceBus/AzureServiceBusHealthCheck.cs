using DddDotNet.Infrastructure.HealthChecks;
using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusHealthCheck : TcpHealthCheck
{
    public AzureServiceBusHealthCheck(string @namespace, IReadOnlyCollection<int> ports)
        : base(@namespace, ports)
    {
    }
}
