using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.GooglePubSub;

public class GooglePubSubHealthCheck : IHealthCheck
{
    private readonly GooglePubSubOptions _options;

    public GooglePubSubHealthCheck(GooglePubSubOptions options)
    {
        _options = options;
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _options.CredentialFilePath);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var publisherService = await PublisherServiceApiClient.CreateAsync(cancellationToken);
            var topicName = new TopicName(_options.ProjectId, _options.TopicId);
            var topic = await publisherService.GetTopicAsync(topicName, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.SubscriptionId))
            {
                var subscriberService = await SubscriberServiceApiClient.CreateAsync(cancellationToken);
                var subscriptionName = new SubscriptionName(_options.ProjectId, _options.SubscriptionId);
                var subscription = await subscriberService.GetSubscriptionAsync(subscriptionName, cancellationToken);
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
