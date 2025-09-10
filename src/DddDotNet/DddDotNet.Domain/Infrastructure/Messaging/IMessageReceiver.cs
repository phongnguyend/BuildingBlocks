using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Domain.Infrastructure.Messaging;

public interface IMessageReceiver<T>
{
    Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default);
}

public class ConsumerHandledException : Exception
{
    public ConsumerHandledExceptionNextAction NextAction { get; set; }
}

public enum ConsumerHandledExceptionNextAction
{
    Retry,
    ReQueue,
    DeadLetter,
}