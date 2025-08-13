using Azure.Messaging.ServiceBus;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusTopicSender<T> : IMessageSender<T>
{
    private readonly AzureServiceBusTopicOptions _options;

    public AzureServiceBusTopicSender(AzureServiceBusTopicOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        await using var client = _options.CreateServiceBusClient();
        ServiceBusSender sender = client.CreateSender(_options.Topic);
        var serviceBusMessage = new ServiceBusMessage(new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.GetBytes());
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
