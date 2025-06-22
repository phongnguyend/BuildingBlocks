using Microsoft.Azure.Management.EventGrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridHealthCheck : IHealthCheck
{
    private readonly AzureEventGridHealthCheckOptions _options;

    public AzureEventGridHealthCheck(AzureEventGridHealthCheckOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var creds = await ApplicationTokenProvider.LoginSilentAsync(_options.AzureActiveDirectoryDomain, _options.ClientId, _options.ClientSecret);
            var client = new EventGridManagementClient(creds) { SubscriptionId = _options.SubscriptionId };
            var domain = await client.Domains.GetAsync(_options.ResourceGroupName, _options.DomainName);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}

public class AzureEventGridHealthCheckOptions
{
    public string AzureActiveDirectoryDomain { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string SubscriptionId { get; set; }

    public string ResourceGroupName { get; set; }

    public string DomainName { get; set; }

    public string DomainEndpoint { get; set; }
}
