using DddDotNet.Domain.Infrastructure.Messaging;
using Google.Cloud.PubSub.V1;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.GooglePubSub;

public class GooglePubSubReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>
{
    private readonly GooglePubSubOptions _options;

    public GooglePubSubReceiver(GooglePubSubOptions options)
    {
        _options = options;
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _options.CredentialFilePath);
    }

    public async Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ReceiveStringAsync(async retrievedMessage =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>(retrievedMessage);
            await action(message.Data, message.MetaData, cancellationToken);
        }, cancellationToken);
    }

    private async Task ReceiveStringAsync(Func<string, Task> action, CancellationToken cancellationToken)
    {
        SubscriptionName subscriptionName = new SubscriptionName(_options.ProjectId, _options.SubscriptionId);
        SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
        await subscriber.StartAsync((msg, cancellationToken) =>
        {
            action(msg.Data.ToStringUtf8());
            return Task.FromResult(SubscriberClient.Reply.Ack);
        });
        await subscriber.StopAsync(TimeSpan.FromSeconds(15));
    }
}
