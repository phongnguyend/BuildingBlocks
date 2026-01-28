using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureEventHub;

public class AzureEventHubReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>, IDisposable
{
    private readonly AzureEventHubOptions _options;

    public AzureEventHubReceiver(AzureEventHubOptions options)
    {
        _options = options;
    }

    public void Dispose()
    {
    }

    public async Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                var messageAsString = Encoding.UTF8.GetString(eventArgs.Data.EventBody);
                var message = JsonSerializer.Deserialize<Message<T>>(messageAsString);
                await action(message.Data, message.MetaData, cancellationToken);
                await eventArgs.UpdateCheckpointAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            try
            {
                Console.WriteLine(eventArgs.Exception);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

        var processor = _options.CreateEventProcessorClient(EventHubConsumerClient.DefaultConsumerGroupName);
        processor.ProcessEventAsync += ProcessEventHandler;
        processor.ProcessErrorAsync += ProcessErrorHandler;
        await processor.StartProcessingAsync(cancellationToken);
    }
}
