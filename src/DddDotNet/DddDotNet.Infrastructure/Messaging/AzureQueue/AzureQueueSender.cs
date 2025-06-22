using Azure.Storage.Queues;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureQueue;

public class AzureQueueSender<T> : IMessageSender<T>
{
    private readonly string _connectionString;
    private readonly string _queueName;
    private readonly QueueMessageEncoding _messageEncoding;

    public AzureQueueSender(string connectionString, string queueName, QueueMessageEncoding messageEncoding = QueueMessageEncoding.None)
    {
        _connectionString = connectionString;
        _queueName = queueName;
        _messageEncoding = messageEncoding;
    }

    public async Task SendAsync(T message, MetaData metaData, CancellationToken cancellationToken = default)
    {
        var queueClient = new QueueClient(_connectionString, _queueName, new QueueClientOptions
        {
            MessageEncoding = _messageEncoding,
        });

        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var jsonMessage = new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject();

        await queueClient.SendMessageAsync(jsonMessage, cancellationToken);
    }
}
