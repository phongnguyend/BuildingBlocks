using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Messaging;

public class AzureServiceBusTopicSenderTests
{
    private static string _connectionString;

    public AzureServiceBusTopicSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _connectionString = config["Messaging:AzureServiceBus:ConnectionString"];
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        for (int i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var topicOptions = new AzureServiceBusTopicOptions
            {
                ConnectionString = _connectionString,
                Topic = "topic-integration-test"
            };
            var sender = new AzureServiceBusTopicSender<Message>(topicOptions);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task TopicHealthCheck_Healthy()
    {
        var topicOptions = new AzureServiceBusTopicOptions
        {
            ConnectionString = _connectionString,
            Topic = "topic-integration-test"
        };
        var healthCheck = new AzureServiceBusTopicHealthCheck(topicOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task TopicHealthCheck_Degraded()
    {
        var topicOptions = new AzureServiceBusTopicOptions
        {
            ConnectionString = _connectionString,
            Topic = Guid.NewGuid().ToString()
        };
        var healthCheck = new AzureServiceBusTopicHealthCheck(topicOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }

    [Fact]
    public async Task SubscriptionHealthCheck_Healthy()
    {
        var subscriptionOptions = new AzureServiceBusSubscriptionOptions
        {
            ConnectionString = _connectionString,
            Topic = "topic-integration-test",
            Subscription = "sub-integration-test"
        };
        var healthCheck = new AzureServiceBusSubscriptionHealthCheck(subscriptionOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task SubscriptionHealthCheck_Degraded()
    {
        var subscriptionOptions = new AzureServiceBusSubscriptionOptions
        {
            ConnectionString = _connectionString,
            Topic = "topic-integration-test",
            Subscription = Guid.NewGuid().ToString()
        };
        var healthCheck = new AzureServiceBusSubscriptionHealthCheck(subscriptionOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
