using CryptographyHelper;
using CryptographyHelper.SymmetricAlgorithms;
using DddDotNet.Domain.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQReceiver<T> : IMessageReceiver<T>, IDisposable
{
    private readonly RabbitMQReceiverOptions _options;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMQReceiver(RabbitMQReceiverOptions options)
    {
        _options = options;
    }

    private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        // TODO: add log here
    }

    public Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default)
    {
        _connection = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true
        }.CreateConnection();

        _connection.ConnectionShutdown += Connection_ConnectionShutdown;

        _channel = _connection.CreateModel();

        if (_options.AutomaticCreateEnabled)
        {
            var arguments = new Dictionary<string, object>();

            if (string.Equals(_options.QueueType, "Quorum", StringComparison.OrdinalIgnoreCase))
            {
                arguments["x-queue-type"] = "quorum";
            }
            else if (string.Equals(_options.QueueType, "Stream", StringComparison.OrdinalIgnoreCase))
            {
                arguments["x-queue-type"] = "stream";
            }

            if (_options.SingleActiveConsumer)
            {
                arguments["x-single-active-consumer"] = true;
            }

            if (_options.DeadLetterEnabled)
            {
                arguments["x-dead-letter-exchange"] = string.Empty;

                var deadLetterQueueName = _options.QueueName + "-dead-letters";

                arguments["x-dead-letter-routing-key"] = deadLetterQueueName;

                _channel.QueueDeclare(deadLetterQueueName, true, false, false, null);
            }

            for (int i = 0; i < _options.MaxRetryCount; i++)
            {
                var queueName = _options.QueueName + "-retry-" + (i + 1);
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-message-ttl", 5000 * (i + 1) },
                    { "x-dead-letter-exchange", string.Empty },
                    { "x-dead-letter-routing-key", _options.QueueName }
                });
            }

            arguments = arguments.Count == 0 ? null : arguments;

            _channel.QueueDeclare(_options.QueueName, true, false, false, arguments);
            _channel.QueueBind(_options.QueueName, _options.ExchangeName, _options.RoutingKey, null);
        }

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var bodyText = string.Empty;

                if (_options.MessageEncryptionEnabled)
                {
                    var parts = Encoding.UTF8.GetString(ea.Body.Span).Split('.');

                    var iv = parts[0].FromBase64String();
                    var encryptedBytes = parts[1].FromBase64String();

                    bodyText = encryptedBytes.UseAES(_options.MessageEncryptionKey.FromBase64String())
                    .WithCipher(CipherMode.CBC)
                    .WithIV(iv)
                    .WithPadding(PaddingMode.PKCS7)
                    .Decrypt()
                    .GetString();
                }
                else
                {
                    bodyText = Encoding.UTF8.GetString(ea.Body.Span);
                }

                var message = JsonSerializer.Deserialize<Message<T>>(bodyText);

                await action(message.Data, message.MetaData);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (ConsumerException ex)
            {
                if (ex.Retryable)
                {
                    if (_options.MaxRetryCount > 0)
                    {
                        int retryCount = GetRetryCount(ea.BasicProperties);

                        if (retryCount < _options.MaxRetryCount)
                        {
                            var props = _channel.CreateBasicProperties();
                            props.Persistent = true;
                            props.Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>();
                            props.Headers["x-retry"] = retryCount + 1;

                            _channel.BasicPublish(string.Empty, _options.QueueName + "-retry-" + (retryCount + 1), props, ea.Body.ToArray());
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            if (_options.DeadLetterEnabled)
                            {
                                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                            }
                            else
                            {
                                // TODO: Log and Stop
                            }
                        }
                    }
                    else
                    {
                        if (_options.DeadLetterEnabled)
                        {
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        }
                        else
                        {
                            // TODO: Log and Stop
                        }
                    }
                }
                else
                {
                    if (_options.DeadLetterEnabled)
                    {
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        // TODO: Log and Stop
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Log and Stop
            }
        };

        _channel.BasicConsume(queue: _options.QueueName,
                             autoAck: false,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private static int GetRetryCount(IBasicProperties props)
    {
        if (props?.Headers != null && props.Headers.TryGetValue("x-retry", out var val))
        {
            if (val is byte[] bytes)
            {
                return int.Parse(Encoding.UTF8.GetString(bytes));
            }

            return Convert.ToInt32(val);
        }

        return 0;
    }
}
