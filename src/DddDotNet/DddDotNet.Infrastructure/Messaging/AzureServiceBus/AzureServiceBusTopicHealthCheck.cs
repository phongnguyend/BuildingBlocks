using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusTopicHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly string _topicName;

    public AzureServiceBusTopicHealthCheck(string connectionString, string topicName)
    {
        _connectionString = connectionString;
        _topicName = topicName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new ServiceBusAdministrationClient(_connectionString);
            var topic = await client.GetTopicAsync(_topicName);

            if (string.Equals(topic?.Value?.Name, _topicName, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus, description: $"Topic: '{_topicName}' doesn't exist");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
