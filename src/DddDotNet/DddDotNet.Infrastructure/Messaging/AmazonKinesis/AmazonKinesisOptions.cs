using Amazon.Kinesis;

namespace DddDotNet.Infrastructure.Messaging.AmazonKinesis;

public class AmazonKinesisOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string StreamName { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonKinesisClient CreateAmazonKinesisClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonKinesisClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonKinesisClient(regionEndpoint);
    }
}
