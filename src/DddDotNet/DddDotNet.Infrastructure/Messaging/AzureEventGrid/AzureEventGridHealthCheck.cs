using DddDotNet.Infrastructure.HealthChecks;
using System;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridHealthCheck : TcpHealthCheck
{
    public AzureEventGridHealthCheck(string endpoint)
        : base(new Uri(endpoint).Host, [443])
    {
    }
}
