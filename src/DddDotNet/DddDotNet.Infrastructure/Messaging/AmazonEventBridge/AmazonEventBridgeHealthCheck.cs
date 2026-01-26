using DddDotNet.Infrastructure.HealthChecks;

namespace DddDotNet.Infrastructure.Messaging.AmazonEventBridge;

public class AmazonEventBridgeHealthCheck : TcpHealthCheck
{
    public AmazonEventBridgeHealthCheck(string eventBridgeServiceEndpoint)
        : base(eventBridgeServiceEndpoint, [443])
    {
    }
}