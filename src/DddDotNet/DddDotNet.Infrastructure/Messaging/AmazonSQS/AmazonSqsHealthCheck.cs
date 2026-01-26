using DddDotNet.Infrastructure.HealthChecks;
using System;

namespace DddDotNet.Infrastructure.Messaging.AmazonSQS;

public class AmazonSqsHealthCheck : TcpHealthCheck
{
    public AmazonSqsHealthCheck(string queueUrl)
        : base(new Uri(queueUrl).Host, [443])
    {
    }
}
