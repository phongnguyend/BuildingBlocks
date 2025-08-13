using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueueStorageHealthCheck : IHealthCheck
{
    private readonly AzureQueueOptions _options;

    public AzureQueueStorageHealthCheck(AzureQueueOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueClient = _options.CreateQueueClient();

            if (!await queueClient.ExistsAsync(cancellationToken))
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: $"Queue '{_options.QueueName}' not exists");
            }

            await queueClient.GetPropertiesAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
