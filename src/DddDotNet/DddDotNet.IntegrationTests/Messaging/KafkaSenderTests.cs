using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Messaging;

public class KafkaSenderTests
{
    private static KafkaOptions _options;

    public KafkaSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _options = new KafkaOptions();

        config.GetSection("Messaging:Kafka").Bind(_options);
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        for (var i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var sender = new KafkaSender<Message>("localhost:9092", "ddddotnet");
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new KafkaHealthCheck("localhost:9092", "healthcheck");
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        var healthCheck = new KafkaHealthCheck("xxxx:9092", "healthcheck");
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
