using System;

namespace DddDotNet.Domain.Infrastructure.Messaging;

public class ConsumerException : Exception
{
    public bool Retryable { get; set; }
}
