using DddDotNet.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DddDotNet.IntegrationTests.Configuration;

public class GoogleCloudSecretManagerConfigurationProviderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    IConfigurationRoot _config;

    public GoogleCloudSecretManagerConfigurationProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        var tempConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .AddGoogleCloudSecretManager(new GoogleCloudSecretManagerOptions
            {
                CredentialFilePath = tempConfig["Configuration:GoogleCloudSecretManager:CredentialFilePath"],
                Parent = tempConfig["Configuration:GoogleCloudSecretManager:Parent"]
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
