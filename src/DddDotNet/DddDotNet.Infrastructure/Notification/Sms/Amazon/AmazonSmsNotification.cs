﻿using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Sms.Amazon;

public class AmazonSmsNotification : ISmsNotification
{
    private readonly AmazonOptions _options;

    public AmazonSmsNotification(AmazonOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(ISmsMessage smsMessage, CancellationToken cancellationToken = default)
    {
        var snsClient = new AmazonSimpleNotificationServiceClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));

        var publishResponse = await snsClient.PublishAsync(new PublishRequest
        {
            Message = smsMessage.Message,
            PhoneNumber = smsMessage.PhoneNumber,
        }, cancellationToken);
    }
}
