﻿using DddDotNet.Domain.Infrastructure.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.Fake;

public class FakeReceiver<T> : IMessageReceiver<T>
{
    public Task ReceiveAsync(Func<T, MetaData, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        // do nothing
        return Task.CompletedTask;
    }
}
