using Amazon.EventBridge;

namespace DddDotNet.Infrastructure.Messaging.AmazonEventBridge;

public class AmazonEventBridgeOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string EndpointId { get; set; }

    public string EndpointName { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonEventBridgeClient CreateAmazonEventBridgeClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonEventBridgeClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonEventBridgeClient(regionEndpoint);
    }
}
