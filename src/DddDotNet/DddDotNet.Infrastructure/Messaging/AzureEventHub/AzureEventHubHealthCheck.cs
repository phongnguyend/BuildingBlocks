using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubHealthCheck : IHealthCheck
{
    private readonly AzureEventHubOptions _options;

    public AzureEventHubHealthCheck(AzureEventHubOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var producer = _options.CreateEventHubProducerClient();
            var properties = await producer.GetEventHubPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
