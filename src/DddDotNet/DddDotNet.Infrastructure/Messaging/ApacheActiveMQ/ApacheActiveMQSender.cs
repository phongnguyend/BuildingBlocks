using Apache.NMS;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;

public class ApacheActiveMQSender<T> : IMessageSender<T>
{
    private readonly ApacheActiveMQOptions _options;

    public ApacheActiveMQSender(ApacheActiveMQOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        Uri connecturi = new Uri(_options.Url);
        IConnectionFactory factory = new NMSConnectionFactory(connecturi);

        using IConnection connection = await factory.CreateConnectionAsync(_options.UserName, _options.Password);
        using ISession session = await connection.CreateSessionAsync();
        IDestination destination = !string.IsNullOrWhiteSpace(_options.TopicName) ?
            await session.GetTopicAsync(_options.TopicName) : await session.GetQueueAsync(_options.QueueName);
        using IMessageProducer producer = session.CreateProducer(destination);

        connection.Start();
        producer.DeliveryMode = MsgDeliveryMode.Persistent;
        producer.RequestTimeout = TimeSpan.FromSeconds(30);

        ITextMessage request = session.CreateTextMessage(new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject());

        await producer.SendAsync(request);
    }
}
