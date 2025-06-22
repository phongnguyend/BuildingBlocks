using DddDotNet.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DddDotNet.IntegrationTests.Infrastructure.Configurations;

public class HashiCorpVaultConfigurationProviderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    IConfigurationRoot _config;

    public HashiCorpVaultConfigurationProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        // vault server -dev -dev-root-token-id=dev-only-token

        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .AddHashiCorpVault(new HashiCorpVaultOptions
            {
                Address = "http://localhost:8200/",
                SecretEnginePath = "secret",
                SecretPath = "DddDotNet",
                AuthMethod = "Token",
                Auth = new HashiCorpVaultAuthOptions
                {
                    Token = new HashiCorpVaultTokenAuthOptions { Token = "dev-only-token" }
                }
            })
            .Build();
    }

    [Fact]
    public Task Test()
    {
        var storageProvider = _config["Storage:Provider"];
        var messageBokerProvider = _config["Messaging:Provider"];

        _testOutputHelper.WriteLine($"Storage:Provider { storageProvider}");
        _testOutputHelper.WriteLine($"Messaging:Provider {messageBokerProvider}");

        return Task.CompletedTask;
    }
}
