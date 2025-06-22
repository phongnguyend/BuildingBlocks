using System;

namespace DddDotNet.Domain.Infrastructure.Messaging;

public class MetaData
{
    public string MessageId { get; set; }

    public string MessageVersion { get; set; }

    public string CorrelationId { get; set; }

    public DateTimeOffset? CreationDateTime { get; set; }

    public DateTimeOffset? EnqueuedDateTime { get; set; }
}
