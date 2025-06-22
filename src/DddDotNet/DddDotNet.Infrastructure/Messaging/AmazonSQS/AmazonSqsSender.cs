using Amazon;
using Amazon.SQS;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonSQS;

public class AmazonSqsSender<T> : IMessageSender<T>
{
    private readonly AmazonSqsOptions _options;

    public AmazonSqsSender(AmazonSqsOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        var sqsClient = new AmazonSQSClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));

        var responseSendMsg = await sqsClient.SendMessageAsync(_options.QueueUrl, new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject());
    }
}
