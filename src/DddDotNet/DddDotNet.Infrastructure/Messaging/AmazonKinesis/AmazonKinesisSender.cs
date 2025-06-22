using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonKinesis;

public class AmazonKinesisSender<T> : IMessageSender<T>
{
    private readonly AmazonKinesisOptions _options;

    public AmazonKinesisSender(AmazonKinesisOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        var kinesisClient = new AmazonKinesisClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));

        var putRecordReponse = await kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = _options.StreamName,
            Data = new MemoryStream(new Message<T>
            {
                Data = message,
                MetaData = metaData,
            }.GetBytes()),
            PartitionKey = $"PartitionKey-{Guid.NewGuid()}",
        });
    }
}
