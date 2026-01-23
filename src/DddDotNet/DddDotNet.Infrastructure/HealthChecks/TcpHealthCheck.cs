using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.HealthChecks;

public class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private IReadOnlyCollection<int> _ports;

    public TcpHealthCheck(string host, IReadOnlyCollection<int> ports)
    {
        _host = host;
        _ports = ports;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        foreach (var port in _ports)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_host, port);
            }
            catch (Exception exception)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"Host: '{_host}', Port: '{port}', Exception: '{exception.Message}'", exception);
            }
        }

        return HealthCheckResult.Healthy($"Host: '{_host}', Ports: '{string.Join(", ", _ports)}'");
    }
}
