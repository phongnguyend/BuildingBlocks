using Apache.NMS;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;

public class ApacheActiveMQReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>
{
    private readonly ApacheActiveMQOptions _options;

    public ApacheActiveMQReceiver(ApacheActiveMQOptions options)
    {
        _options = options;
    }

    public Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        Uri connecturi = new Uri(_options.Url);
        IConnectionFactory factory = new NMSConnectionFactory(connecturi);
        IConnection connection = factory.CreateConnection(_options.UserName, _options.Password);
        ISession session = connection.CreateSession(!string.IsNullOrWhiteSpace(_options.TopicName) ? AcknowledgementMode.AutoAcknowledge : AcknowledgementMode.IndividualAcknowledge);
        IMessageConsumer consumer = null;

        if (!string.IsNullOrWhiteSpace(_options.QueueName))
        {
            consumer = session.CreateConsumer(session.GetQueue(_options.QueueName));
        }
        else if (!string.IsNullOrWhiteSpace(_options.TopicName))
        {
            if (string.IsNullOrWhiteSpace(_options.SubscriberName))
            {
                consumer = session.CreateConsumer(session.GetTopic(_options.TopicName));
            }
            else
            {
                if (_options.SharedDurableSubscriber)
                {
                    session.CreateSharedDurableConsumer(session.GetTopic(_options.TopicName), _options.SubscriberName); // not supported yet
                }
                else
                {
                    consumer = session.CreateDurableConsumer(session.GetTopic(_options.TopicName), _options.SubscriberName);
                }
            }
        }

        connection.Start();

        consumer.Listener += (IMessage retrievedMessage) =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>((retrievedMessage as ITextMessage).Text);
            action(message.Data, message.MetaData, cancellationToken).Wait();
            retrievedMessage.Acknowledge();
        };

        return Task.CompletedTask;
    }
}
