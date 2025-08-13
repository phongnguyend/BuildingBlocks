using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueueSender<T> : IMessageSender<T>
{
    private readonly AzureQueueOptions _options;

    public AzureQueueSender(AzureQueueOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        var queueClient = _options.CreateQueueClient();

        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var jsonMessage = new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject();

        await queueClient.SendMessageAsync(jsonMessage, cancellationToken);
    }
}
