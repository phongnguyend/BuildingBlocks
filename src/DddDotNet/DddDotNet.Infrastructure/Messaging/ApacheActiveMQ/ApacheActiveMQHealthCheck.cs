using Apache.NMS;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;

public class ApacheActiveMQHealthCheck : IHealthCheck
{
    private readonly ApacheActiveMQOptions _options;

    public ApacheActiveMQHealthCheck(ApacheActiveMQOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Uri connecturi = new Uri(_options.Url);
            IConnectionFactory factory = new NMSConnectionFactory(connecturi);

            using IConnection connection = await factory.CreateConnectionAsync(_options.UserName, _options.Password);
            using ISession session = await connection.CreateSessionAsync();
            connection.Start();

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
