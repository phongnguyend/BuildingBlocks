namespace DddDotNet.Infrastructure.Messaging.AmazonKinesis;

public class AmazonKinesisOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string StreamName { get; set; }

    public string RegionEndpoint { get; set; }
}
