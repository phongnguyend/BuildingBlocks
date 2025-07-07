using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonSNS;

public class AmazonSnsSender<T> : IMessageSender<T>
{
    private readonly AmazonSnsOptions _options;

    public AmazonSnsSender(AmazonSnsOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        var snsClient = _options.CreateAmazonSimpleNotificationServiceClient();

        var publishResponse = await snsClient.PublishAsync(_options.TopicARN, new Message<T>
        {
            Data = message,
            MetaData = metaData,
        }.SerializeObject());
    }
}
