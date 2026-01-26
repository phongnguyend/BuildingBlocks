using DddDotNet.Infrastructure.HealthChecks;

namespace DddDotNet.Infrastructure.Messaging.AmazonSNS;

public class AmazonSnsHealthCheck : TcpHealthCheck
{
    public AmazonSnsHealthCheck(string snsServiceEndpoint)
        : base(snsServiceEndpoint, [443])
    {
    }
}
