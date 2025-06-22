using Confluent.Kafka;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.Kafka;

public class KafkaReceiver<T> : IMessageReceiver<T>, IDisposable
{
    private readonly KafkaReceiverOptions _options;
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaReceiver(KafkaReceiverOptions options)
    {
        _options = options;
        var config = new ConsumerConfig
        {
            GroupId = _options.GroupId,
            BootstrapServers = _options.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _options.AutoCommitEnabled,
            EnableAutoOffsetStore = _options.AutoCommitEnabled,
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(_options.Topic);
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }

    public async Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);

                if (consumeResult.IsPartitionEOF)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<Message<T>>(consumeResult.Message.Value);
                await action(message.Data, message.MetaData);

                if (_options.AutoCommitEnabled.HasValue && !_options.AutoCommitEnabled.Value)
                {
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Consume error: {e.Error.Reason}");
            }
        }
    }
}
