using Azure.Messaging.ServiceBus;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusQueueSender<T> : IMessageSender<T>
{
    private readonly AzureServiceBusQueueOptions _options;

    public AzureServiceBusQueueSender(AzureServiceBusQueueOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        await using var client = _options.CreateServiceBusClient();
        ServiceBusSender sender = client.CreateSender(_options.QueueName);
        var serviceBusMessage = new ServiceBusMessage(new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.GetBytes());
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
