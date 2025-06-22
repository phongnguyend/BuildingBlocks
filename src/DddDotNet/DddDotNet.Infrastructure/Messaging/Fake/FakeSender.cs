using DddDotNet.Domain.Infrastructure.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.Fake;

public class FakeSender<T> : IMessageSender<T>
{
    public Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
