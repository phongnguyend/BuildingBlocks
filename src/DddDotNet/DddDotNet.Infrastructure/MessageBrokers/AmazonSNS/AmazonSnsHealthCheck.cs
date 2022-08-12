﻿using Amazon;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.MessageBrokers.AmazonSNS
{
    public class AmazonSnsHealthCheck : IHealthCheck
    {
        private readonly AmazonSnsOptions _options;

        public AmazonSnsHealthCheck(AmazonSnsOptions options)
        {
            _options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var snsClient = new AmazonSimpleNotificationServiceClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));
                var attributes = await snsClient.GetTopicAttributesAsync(_options.TopicARN, cancellationToken);
                if (attributes?.HttpStatusCode == HttpStatusCode.OK)
                {
                    var subscriptions = await snsClient.ListSubscriptionsByTopicAsync(_options.TopicARN, cancellationToken);

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
}
