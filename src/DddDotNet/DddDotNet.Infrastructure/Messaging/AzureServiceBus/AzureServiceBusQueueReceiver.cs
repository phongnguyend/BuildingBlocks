using Azure.Messaging.ServiceBus;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusQueueReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>
{
    private readonly AzureServiceBusQueueOptions _options;

    public AzureServiceBusQueueReceiver(AzureServiceBusQueueOptions options)
    {
        _options = options;
    }

    public async Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ReceiveBinaryDataAsync(async retrievedMessage =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>(retrievedMessage);
            await action(message.Data, message.MetaData, cancellationToken);
        }, cancellationToken);
    }

    private async Task ReceiveStringAsync(Func<string, Task> action, CancellationToken cancellationToken)
    {
        await ReceiveBinaryDataAsync(async retrievedMessage =>
        {
            await action(retrievedMessage.ToString());
        }, cancellationToken);
    }

    private async Task ReceiveBinaryDataAsync(Func<BinaryData, Task> action, CancellationToken cancellationToken)
    {
        await using var client = _options.CreateServiceBusClient();
        ServiceBusReceiver receiver = client.CreateReceiver(_options.QueueName);

        while (!cancellationToken.IsCancellationRequested)
        {
            var retrievedMessage = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (retrievedMessage != null)
            {
                await action(retrievedMessage.Body);
                await receiver.CompleteMessageAsync(retrievedMessage, cancellationToken);
            }
            else
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
