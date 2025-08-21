using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DddDotNet.Infrastructure.Configuration;

internal class AwsSecretsManagerConfigurationProvider : ConfigurationProvider
{
    private readonly AwsSecretsManagerOptions _options;

    public AwsSecretsManagerConfigurationProvider(AwsSecretsManagerOptions options)
    {
        _options = options;
    }

    public override void Load()
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _options.SecretName
        };

        using var client = _options.CreateAmazonSecretsManagerClient();
        var response = client.GetSecretValueAsync(request).Result;

        string secretString;
        if (response.SecretString != null)
        {
            secretString = response.SecretString;
        }
        else
        {
            var memoryStream = response.SecretBinary;
            var reader = new StreamReader(memoryStream);
            secretString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString);

        foreach (var item in data)
        {
            Set(item.Key.Replace("__", ":"), item.Value);
        }
    }
}

public class AwsSecretsManagerOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string SecretName { get; set; }

    public string RegionEndpoint { get; set; }

    public AmazonSecretsManagerClient CreateAmazonSecretsManagerClient()
    {
        var regionEndpoint = global::Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint);

        if (!string.IsNullOrWhiteSpace(AccessKeyID))
        {
            return new AmazonSecretsManagerClient(AccessKeyID, SecretAccessKey, regionEndpoint);
        }

        return new AmazonSecretsManagerClient(regionEndpoint);
    }
}

public class AwsSecretsManagerConfigurationSource : IConfigurationSource
{
    private readonly AwsSecretsManagerOptions _options;

    public AwsSecretsManagerConfigurationSource(AwsSecretsManagerOptions options)
    {
        _options = options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new AwsSecretsManagerConfigurationProvider(_options);
    }
}

public static class AwsSecretsManagerConfigurationExtensions
{
    public static IConfigurationBuilder AddAwsSecretsManager(this IConfigurationBuilder builder, AwsSecretsManagerOptions options)
    {
        return builder.Add(new AwsSecretsManagerConfigurationSource(options));
    }
}
