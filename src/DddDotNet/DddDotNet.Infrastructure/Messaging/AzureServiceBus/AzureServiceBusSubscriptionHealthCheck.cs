using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusSubscriptionHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public AzureServiceBusSubscriptionHealthCheck(string connectionString, string topicName, string subscriptionName)
    {
        _connectionString = connectionString;
        _topicName = topicName;
        _subscriptionName = subscriptionName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new ServiceBusAdministrationClient(_connectionString);
            var subscription = await client.GetSubscriptionAsync(_topicName, _subscriptionName);

            if (string.Equals(subscription?.Value?.TopicName, _topicName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(subscription?.Value?.SubscriptionName, _subscriptionName, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Subscription: '{_subscriptionName}' doesn't exist");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
