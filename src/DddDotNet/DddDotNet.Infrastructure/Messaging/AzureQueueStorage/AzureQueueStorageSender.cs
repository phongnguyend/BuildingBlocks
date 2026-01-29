using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureQueueStorage;

public class AzureQueueStorageSender<T> : IMessageSender<T>
{
    private readonly AzureQueueStorageOptions _options;

    public AzureQueueStorageSender(AzureQueueStorageOptions options)
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
