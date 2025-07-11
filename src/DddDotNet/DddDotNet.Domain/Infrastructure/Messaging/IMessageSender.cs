﻿using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Domain.Infrastructure.Messaging;

public interface IMessageSender<T>
{
    Task SendAsync(T message, MetaData metaData = null, CancellationToken cancellationToken = default);
}
