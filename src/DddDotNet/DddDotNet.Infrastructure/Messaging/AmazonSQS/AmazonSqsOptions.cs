namespace DddDotNet.Infrastructure.Messaging.AmazonSQS;

public class AmazonSqsOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string QueueUrl { get; set; }

    public string RegionEndpoint { get; set; }
}
