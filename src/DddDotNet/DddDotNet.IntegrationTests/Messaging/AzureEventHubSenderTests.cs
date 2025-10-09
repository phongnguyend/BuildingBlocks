using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AzureEventHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Messaging;

public class AzureEventHubSenderTests
{
    private static string _connectionString;

    public AzureEventHubSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _connectionString = config["Messaging:AzureEventHub:ConnectionString"];
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        for (var i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var hubOptions = new AzureEventHubOptions
            {
                ConnectionString = _connectionString,
                HubName = "integration-test"
            };
            var sender = new AzureEventHubSender<Message>(hubOptions);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var hubOptions = new AzureEventHubOptions
        {
            ConnectionString = _connectionString,
            HubName = "integration-test"
        };
        var healthCheck = new AzureEventHubHealthCheck(hubOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        var hubOptions = new AzureEventHubOptions
        {
            ConnectionString = _connectionString,
            HubName = "integration-test-not-exist"
        };
        var healthCheck = new AzureEventHubHealthCheck(hubOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
