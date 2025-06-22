namespace DddDotNet.Infrastructure.Messaging.AmazonSNS;

public class AmazonSnsOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string TopicARN { get; set; }

    public string RegionEndpoint { get; set; }
}
