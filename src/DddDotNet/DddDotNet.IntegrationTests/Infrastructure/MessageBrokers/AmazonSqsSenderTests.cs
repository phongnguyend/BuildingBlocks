﻿using DddDotNet.Domain.Infrastructure.MessageBrokers;
using DddDotNet.Infrastructure.MessageBrokers.AmazonSQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.MessageBrokers
{
    public class AmazonSqsSenderTests
    {
        private AmazonSqsOptions _options;

        public AmazonSqsSenderTests()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
                .Build();

            _options = new AmazonSqsOptions();

            config.GetSection("MessageBroker:AmazonSQS").Bind(_options);
        }

        [Fact]
        public async Task SendAsync_Success()
        {
            for (int i = 0; i < 10; i++)
            {
                var message = Message.GetTestMessage();
                var metaData = new MetaData { };
                var sender = new AmazonSqsSender<Message>(_options);
                await sender.SendAsync(message, metaData);
            }
        }

        [Fact]
        public async Task HealthCheck_Healthy()
        {
            var healthCheck = new AmazonSqsHealthCheck(_options);
            var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
            Assert.Equal(HealthStatus.Healthy, checkResult.Status);
        }

        [Fact]
        public async Task HealthCheck_Degraded()
        {
            _options.QueueUrl += "abc";
            var healthCheck = new AmazonSqsHealthCheck(_options);
            var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
            Assert.Equal(HealthStatus.Degraded, checkResult.Status);
        }
    }
}
