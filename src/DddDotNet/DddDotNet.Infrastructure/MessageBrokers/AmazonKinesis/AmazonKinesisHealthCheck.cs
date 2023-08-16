﻿using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.MessageBrokers.AmazonKinesis;

public class AmazonKinesisHealthCheck : IHealthCheck
{
    private readonly AmazonKinesisOptions _options;

    public AmazonKinesisHealthCheck(AmazonKinesisOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var kinesisClient = new AmazonKinesisClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));
            var shardsResponse = await kinesisClient.ListShardsAsync(new ListShardsRequest { StreamName = _options.StreamName });

            if (shardsResponse?.HttpStatusCode == HttpStatusCode.OK)
            {
                return HealthCheckResult.Healthy();
            }
            else
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"HttpStatusCode: {shardsResponse?.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
