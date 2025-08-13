using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusQueueHealthCheck : IHealthCheck
{
    private readonly AzureServiceBusQueueOptions _options;

    public AzureServiceBusQueueHealthCheck(AzureServiceBusQueueOptions options)
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
            var queue = await client.GetQueueAsync(_options.QueueName, cancellationToken);

            if (string.Equals(queue?.Value?.Name, _options.QueueName, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Queue: '{_options.QueueName}' doesn't exist");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
