using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureQueueStorage;

public class AzureQueueStorageReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>
{
    private readonly AzureQueueStorageOptions _options;

    public AzureQueueStorageReceiver(AzureQueueStorageOptions options)
    {
        _options = options;
    }

    public async Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ReceiveStringAsync(async retrievedMessage =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>(retrievedMessage);
            await action(message.Data, message.MetaData, cancellationToken);
        }, cancellationToken);
    }

    public async Task ReceiveStringAsync(Func<string, Task> action, CancellationToken cancellationToken = default)
    {
        var queueClient = _options.CreateQueueClient();
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var retrievedMessages = (await queueClient.ReceiveMessagesAsync(cancellationToken)).Value;

                if (retrievedMessages.Length > 0)
                {
                    foreach (var retrievedMessage in retrievedMessages)
                    {
                        await action(retrievedMessage.Body.ToString());
                        await queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
