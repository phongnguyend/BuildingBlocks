﻿using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly string _hubName;

    public AzureEventHubHealthCheck(string connectionString, string hubName)
    {
        _connectionString = connectionString;
        _hubName = hubName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var producer = new EventHubProducerClient(_connectionString, _hubName);
            var properties = await producer.GetEventHubPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
