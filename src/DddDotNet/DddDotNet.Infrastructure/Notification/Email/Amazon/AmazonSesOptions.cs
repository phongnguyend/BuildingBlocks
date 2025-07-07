using Amazon.SimpleEmail;

namespace DddDotNet.Infrastructure.Notification.Email.Amazon;

public class AmazonSesOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string RegionEndpoint { get; set; }

    public string FromEmail { get; set; }

    public AmazonSimpleEmailServiceClient CreateAmazonSimpleEmailServiceClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonSimpleEmailServiceClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonSimpleEmailServiceClient(regionEndpoint);
    }
}
