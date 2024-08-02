﻿using CryptographyHelper;
using CryptographyHelper.SymmetricAlgorithms;
using DddDotNet.Domain.Infrastructure.MessageBrokers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.MessageBrokers.RabbitMQ;

public class RabbitMQReceiver<T> : IMessageReceiver<T>, IDisposable
{
    private readonly RabbitMQReceiverOptions _options;
    private readonly IConnection _connection;
    private IModel _channel;
    private string _queueName;

    public RabbitMQReceiver(RabbitMQReceiverOptions options)
    {
        _options = options;

        _connection = new ConnectionFactory
        {
            HostName = options.HostName,
            UserName = options.UserName,
            Password = options.Password,
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true
        }.CreateConnection();

        _queueName = options.QueueName;

        _connection.ConnectionShutdown += Connection_ConnectionShutdown;
    }

    private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        // TODO: add log here
    }

    public Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default)
    {
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

            if (_options.DeadLetter != null)
            {
                if (!string.IsNullOrEmpty(_options.DeadLetter.ExchangeName))
                {
                    arguments["x-dead-letter-exchange"] = _options.DeadLetter.ExchangeName;
                }

                if (!string.IsNullOrEmpty(_options.DeadLetter.RoutingKey))
                {
                    arguments["x-dead-letter-routing-key"] = _options.DeadLetter.RoutingKey;
                }

                if (_options.DeadLetter.AutomaticCreateEnabled && !string.IsNullOrEmpty(_options.DeadLetter.QueueName))
                {
                    _channel.QueueDeclare(_options.DeadLetter.QueueName, true, false, false, null);
                    _channel.QueueBind(_options.DeadLetter.QueueName, _options.DeadLetter.ExchangeName, _options.DeadLetter.RoutingKey, null);
                }
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
            catch (Exception ex)
            {
                // TODO: log here
                await Task.Delay(1000);
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: _options.RequeueOnFailure);
            }
        };

        _channel.BasicConsume(queue: _queueName,
                             autoAck: false,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
