using Azure.Messaging.ServiceBus;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusSubscriptionReceiver<T> : IMessageReceiver<T>
{
    private readonly AzureServiceBusSubscriptionOptions _options;

    public AzureServiceBusSubscriptionReceiver(AzureServiceBusSubscriptionOptions options)
    {
        _options = options;
    }

    public async Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default)
    {
        await ReceiveStringAsync(async retrievedMessage =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>(retrievedMessage);
            await action(message.Data, message.MetaData);
        }, cancellationToken);
    }

    private async Task ReceiveStringAsync(Func<string, Task> action, CancellationToken cancellationToken)
    {
        await using var client = _options.CreateServiceBusClient();
        ServiceBusReceiver receiver = client.CreateReceiver(_options.Topic, _options.Subscription);

        while (!cancellationToken.IsCancellationRequested)
        {
            var retrievedMessage = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (retrievedMessage != null)
            {
                await action(Encoding.UTF8.GetString(retrievedMessage.Body));
                await receiver.CompleteMessageAsync(retrievedMessage, cancellationToken);
            }
            else
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
