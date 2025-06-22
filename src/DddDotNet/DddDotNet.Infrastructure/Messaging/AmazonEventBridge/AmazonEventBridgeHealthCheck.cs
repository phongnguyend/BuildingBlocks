using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Messaging.AmazonEventBridge;

public class AmazonEventBridgeHealthCheck : IHealthCheck
{
    private readonly AmazonEventBridgeOptions _options;

    public AmazonEventBridgeHealthCheck(AmazonEventBridgeOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventBridgeClient = new AmazonEventBridgeClient(_options.AccessKeyID, _options.SecretAccessKey, RegionEndpoint.GetBySystemName(_options.RegionEndpoint));
            var endpointResponse = await eventBridgeClient.DescribeEndpointAsync(new DescribeEndpointRequest { HomeRegion = _options.RegionEndpoint, Name = _options.EndpointName }, cancellationToken);

            if (endpointResponse?.HttpStatusCode == HttpStatusCode.OK)
            {
                return HealthCheckResult.Healthy();
            }
            else
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"HttpStatusCode: {endpointResponse?.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
