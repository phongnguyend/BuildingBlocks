using DddDotNet.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DddDotNet.IntegrationTests.Infrastructure.Configurations;

public class AwsSecretsManagerConfigurationProviderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    IConfigurationRoot _config;

    public AwsSecretsManagerConfigurationProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        var tempConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .AddAwsSecretsManager(new AwsSecretsManagerOptions
            {
                AccessKeyID = tempConfig["Configuration:AwsSecretsManager:AccessKeyID"],
                SecretAccessKey = tempConfig["Configuration:AwsSecretsManager:SecretAccessKey"],
                SecretName = tempConfig["Configuration:AwsSecretsManager:SecretName"],
                RegionEndpoint = tempConfig["Configuration:AwsSecretsManager:RegionEndpoint"]
            })
            .Build();
    }

    [Fact]
    public Task Test()
    {
        var storageProvider = _config["Storage:Provider"];
        var messageBokerProvider = _config["Messaging:Provider"];

        _testOutputHelper.WriteLine($"Storage:Provider {storageProvider}");
        _testOutputHelper.WriteLine($"Messaging:Provider {messageBokerProvider}");

        return Task.CompletedTask;
    }
}
