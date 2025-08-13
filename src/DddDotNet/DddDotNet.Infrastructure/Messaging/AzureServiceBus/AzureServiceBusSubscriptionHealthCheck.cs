using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusSubscriptionHealthCheck : IHealthCheck
{
    private readonly AzureServiceBusSubscriptionOptions _options;

    public AzureServiceBusSubscriptionHealthCheck(AzureServiceBusSubscriptionOptions options)
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
            var subscription = await client.GetSubscriptionAsync(_options.Topic, _options.Subscription, cancellationToken);

            if (string.Equals(subscription?.Value?.TopicName, _options.Topic, StringComparison.OrdinalIgnoreCase)
                && string.Equals(subscription?.Value?.SubscriptionName, _options.Subscription, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Subscription: '{_options.Subscription}' doesn't exist");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
