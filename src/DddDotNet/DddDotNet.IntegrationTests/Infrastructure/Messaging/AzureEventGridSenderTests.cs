using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AzureEventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Messaging;

public class AzureEventGridSenderTests
{
    private static string _domainEndpoint;
    private static string _domainKey;
    AzureEventGridHealthCheckOptions _healthCheckOptions;

    public AzureEventGridSenderTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _domainEndpoint = config["Messaging:AzureEventGrid:DomainEndpoint"];
        _domainKey = config["Messaging:AzureEventGrid:DomainKey"];

        _healthCheckOptions = new AzureEventGridHealthCheckOptions
        {
            DomainEndpoint = _domainEndpoint,
        };

        config.GetSection("Messaging:AzureEventGrid:HealthCheck").Bind(_healthCheckOptions);
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        for (int i = 0; i < 10; i++)
        {
            var message = Message.GetTestMessage();
            var metaData = new MetaData { };
            var gridOptions = new AzureEventGridOptions
            {
                DomainEndpoint = _domainEndpoint,
                DomainKey = _domainKey,
                Topic = "integration-test"
            };
            var sender = new AzureEventGridSender<Message>(gridOptions);
            await sender.SendAsync(message, metaData);
        }
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new AzureEventGridHealthCheck(_healthCheckOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        _healthCheckOptions.DomainName += "abc";
        var healthCheck = new AzureEventGridHealthCheck(_healthCheckOptions);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
