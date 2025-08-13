using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusTopicHealthCheck : IHealthCheck
{
    private readonly AzureServiceBusTopicOptions _options;

    public AzureServiceBusTopicHealthCheck(AzureServiceBusTopicOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = !string.IsNullOrWhiteSpace(_options.ConnectionString)
                ? new ServiceBusAdministrationClient(_options.ConnectionString)
                : new ServiceBusAdministrationClient(_options.Namespace, new Azure.Identity.DefaultAzureCredential());
            var topic = await client.GetTopicAsync(_options.Topic, cancellationToken);

            if (string.Equals(topic?.Value?.Name, _options.Topic, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Topic: '{_options.Topic}' doesn't exist");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
