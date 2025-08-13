﻿using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Messaging;

public class AzureServiceBusQueueSenderTests
{
    private static string _connectionString;

    public AzureServiceBusQueueSenderTests()
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
            var queueOptions = new AzureServiceBusQueueOptions
            {
                ConnectionString = _connectionString,
                QueueName = "integration-test"
            };
            var sender = new AzureServiceBusQueueSender<Message>(queueOptions);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var queueOptions = new AzureServiceBusQueueOptions
        {
            ConnectionString = _connectionString,
            QueueName = "integration-test"
        };
        var healthCheck = new AzureServiceBusQueueHealthCheck(queueOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        var queueOptions = new AzureServiceBusQueueOptions
        {
            ConnectionString = _connectionString,
            QueueName = Guid.NewGuid().ToString()
        };
        var healthCheck = new AzureServiceBusQueueHealthCheck(queueOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
