﻿using Azure.Messaging.ServiceBus;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AzureServiceBus;

public class AzureServiceBusQueueReceiver<T> : IMessageReceiver<T>
{
    private readonly string _connectionString;
    private readonly string _queueName;

    public AzureServiceBusQueueReceiver(string connectionString, string queueName)
    {
        _connectionString = connectionString;
        _queueName = queueName;
    }

    public async Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default)
    {
        await ReceiveStringAsync(async retrievedMessage =>
        {
            var message = JsonSerializer.Deserialize<Message<T>>(retrievedMessage);
            await action(message.Data, message.MetaData);
        }, cancellationToken);
    }

    private async Task ReceiveStringAsync(Func<string, Task> action, CancellationToken cancellationToken)
    {
        await using var client = new ServiceBusClient(_connectionString);
        ServiceBusReceiver receiver = client.CreateReceiver(_queueName);

        while (!cancellationToken.IsCancellationRequested)
        {
            var retrievedMessage = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (retrievedMessage != null)
            {
                await action(Encoding.UTF8.GetString(retrievedMessage.Body));
                await receiver.CompleteMessageAsync(retrievedMessage, cancellationToken);
            }
            else
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
