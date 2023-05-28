﻿using Amazon;
using Amazon.SQS;
using DddDotNet.Domain.Infrastructure.MessageBrokers;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.MessageBrokers.AmazonSQS;

public class AmazonSqsReceiver<T> : IMessageReceiver<T>
{
    private readonly AmazonSqsOptions _options;

    public AmazonSqsReceiver(AmazonSqsOptions options)
    {
        _options = options;
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
        var sqsClient = new AmazonSQSClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var retrievedMessages = await sqsClient.ReceiveMessageAsync(_options.QueueUrl);

                if (retrievedMessages.Messages.Count > 0)
                {
                    foreach (var retrievedMessage in retrievedMessages.Messages)
                    {
                        await action(retrievedMessage.Body);
                        await sqsClient.DeleteMessageAsync(_options.QueueUrl, retrievedMessage.ReceiptHandle);
                    }
                }
                else
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
