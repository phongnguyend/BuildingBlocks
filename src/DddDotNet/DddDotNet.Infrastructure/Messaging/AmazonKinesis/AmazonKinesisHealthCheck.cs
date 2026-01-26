using DddDotNet.Infrastructure.HealthChecks;

namespace DddDotNet.Infrastructure.Messaging.AmazonKinesis;

public class AmazonKinesisHealthCheck : TcpHealthCheck
{
    public AmazonKinesisHealthCheck(string kinesisServiceEndpoint)
        : base(kinesisServiceEndpoint, [443])
    {
    }
}
