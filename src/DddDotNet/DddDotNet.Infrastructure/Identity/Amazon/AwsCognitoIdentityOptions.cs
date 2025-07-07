using Amazon.CognitoIdentityProvider;

namespace DddDotNet.Infrastructure.Identity.GoogleCloud;

public class AwsCognitoIdentityOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string UserPoolID { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonCognitoIdentityProviderClient CreateAmazonCognitoIdentityProviderClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonCognitoIdentityProviderClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonCognitoIdentityProviderClient(regionEndpoint);
    }
}
