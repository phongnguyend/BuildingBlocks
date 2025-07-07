using Amazon.SQS;

namespace DddDotNet.Infrastructure.Messaging.AmazonSQS;

public class AmazonSqsOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string QueueUrl { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonSQSClient CreateAmazonSQSClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonSQSClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonSQSClient(regionEndpoint);
    }
}
