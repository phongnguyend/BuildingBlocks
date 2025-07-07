using Amazon.SimpleNotificationService;

namespace DddDotNet.Infrastructure.Messaging.AmazonSNS;

public class AmazonSnsOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string TopicARN { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonSimpleNotificationServiceClient CreateAmazonSimpleNotificationServiceClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonSimpleNotificationServiceClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonSimpleNotificationServiceClient(regionEndpoint);
    }
}
