using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonSQS;

public class AmazonSqsHealthCheck : IHealthCheck
{
    private readonly AmazonSqsOptions _options;

    public AmazonSqsHealthCheck(AmazonSqsOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var sqsClient = new AmazonSQSClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));
            var attributes = await sqsClient.GetQueueAttributesAsync(_options.QueueUrl, new List<string> { "All" }, cancellationToken);

            if (attributes?.HttpStatusCode == HttpStatusCode.OK)
            {
                return HealthCheckResult.Healthy();
            }
            else
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"HttpStatusCode: {attributes?.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
