using DddDotNet.Domain.Infrastructure.Messaging;
using Google.Cloud.PubSub.V1;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.GooglePubSub;

public class GooglePubSubSender<T> : IMessageSender<T>
{
    private readonly GooglePubSubOptions _options;

    public GooglePubSubSender(GooglePubSubOptions options)
    {
        _options = options;
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _options.CredentialFilePath);
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        TopicName topicName = new TopicName(_options.ProjectId, _options.TopicId);
        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
        string messageId = await publisher.PublishAsync(new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject());
        await publisher.ShutdownAsync(TimeSpan.FromSeconds(15));
    }
}
