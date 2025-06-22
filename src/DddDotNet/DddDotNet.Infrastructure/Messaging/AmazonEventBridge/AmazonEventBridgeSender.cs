using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using DddDotNet.Domain.Infrastructure.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonEventBridge;

public class AmazonEventBridgeSender<T> : IMessageSender<T>
{
    private readonly AmazonEventBridgeOptions _options;

    public AmazonEventBridgeSender(AmazonEventBridgeOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        var eventBridgeClient = new AmazonEventBridgeClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));

        var putEventsReponse = await eventBridgeClient.PutEventsAsync(new PutEventsRequest
        {
            EndpointId = _options.EndpointId,
            Entries = new List<PutEventsRequestEntry>
            {
                new PutEventsRequestEntry
                {
                    Detail = new Message<T>
                    {
                        Data = message,
                        MetaData = metaData,
                    }.SerializeObject(),
                    DetailType = typeof(T).FullName,
                    Source = "DddDotNet",
                },
            },
        });
    }
}
