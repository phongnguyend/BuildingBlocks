using Azure.Identity;
using Microsoft.Graph;

namespace DddDotNet.Infrastructure.Storages.SharePointOnline;

public class SharePointOnlineOptions
{
    public string ClientId { get; set; }

    public string TenantId { get; set; }

    public string ClientSecret { get; set; }

    public string SiteHostname { get; set; }

    public string SitePath { get; set; }

    public string DocumentLibraryName { get; set; }

    public string Path { get; set; }

    public GraphServiceClient CreateGraphServiceClient()
    {
        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var clientSecretCredential = new ClientSecretCredential(
            TenantId,
            ClientId,
            ClientSecret,
            options);

        var graphServiceClient = new GraphServiceClient(clientSecretCredential);

        return graphServiceClient;
    }
}