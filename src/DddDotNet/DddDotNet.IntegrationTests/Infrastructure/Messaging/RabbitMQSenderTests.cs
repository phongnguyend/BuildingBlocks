using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Messaging;

public class RabbitMQSenderTests
{
    private static RabbitMQSenderOptions _rabbitMQSenderOptions;

    public RabbitMQSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _rabbitMQSenderOptions = new RabbitMQSenderOptions();

        config.GetSection("Messaging:RabbitMQ").Bind(_rabbitMQSenderOptions);

        _rabbitMQSenderOptions.MessageEncryptionKey = "KEhv7V8VedlhVlNr5vQstLk99l5uflYGB5lamGZd4R4=";
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        _rabbitMQSenderOptions.MessageEncryptionEnabled = false;
        for (int i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var sender = new RabbitMQSender<Message>(_rabbitMQSenderOptions);
            await sender.SendAsync(message, metaData);
        }

        _rabbitMQSenderOptions.MessageEncryptionEnabled = true;
        for (int i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var sender = new RabbitMQSender<Message>(_rabbitMQSenderOptions);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new RabbitMQHealthCheck(new RabbitMQHealthCheckOptions
        {
            HostName = _rabbitMQSenderOptions.HostName,
            UserName = _rabbitMQSenderOptions.UserName,
            Password = _rabbitMQSenderOptions.Password,
        });
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        var healthCheck = new RabbitMQHealthCheck(new RabbitMQHealthCheckOptions
        {
            HostName = _rabbitMQSenderOptions.HostName,
            UserName = Guid.NewGuid().ToString(),
            Password = Guid.NewGuid().ToString(),
        });
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
