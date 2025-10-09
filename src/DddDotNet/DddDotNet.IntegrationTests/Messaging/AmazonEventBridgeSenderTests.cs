using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AmazonEventBridge;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Messaging;

public class AmazonEventBridgeSenderTests
{
    private AmazonEventBridgeOptions _options;

    public AmazonEventBridgeSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _options = new AmazonEventBridgeOptions();

        config.GetSection("Messaging:AmazonEventBridge").Bind(_options);
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        for (var i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var sender = new AmazonEventBridgeSender<Message>(_options);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        _options.EndpointName = "ddddotnet";
        var healthCheck = new AmazonEventBridgeHealthCheck(_options);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        _options.EndpointName = "xxx";
        var healthCheck = new AmazonEventBridgeHealthCheck(_options);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
