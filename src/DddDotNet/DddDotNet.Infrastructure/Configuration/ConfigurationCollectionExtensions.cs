using Amazon;
using Amazon.Runtime;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;

namespace DddDotNet.Infrastructure.Configuration;

public static class ConfigurationCollectionExtensions
{
    public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder configurationBuilder, ConfigurationProviders options)
    {
        if (options?.SqlServer?.IsEnabled ?? false)
        {
            configurationBuilder.AddSqlServer(options.SqlServer);
        }

        if (options?.AzureAppConfiguration?.IsEnabled ?? false)
        {
            if (!string.IsNullOrEmpty(options.AzureAppConfiguration.ConnectionString))
            {
                configurationBuilder.AddAzureAppConfiguration(options.AzureAppConfiguration.ConnectionString);
            }
            else
            {
                configurationBuilder.AddAzureAppConfiguration(opt =>
                {
                    opt.Connect(new Uri(options.AzureAppConfiguration.Endpoint), new DefaultAzureCredential());
                });
            }
        }

        if (options?.AzureKeyVault?.IsEnabled ?? false)
        {
            configurationBuilder.AddAzureKeyVault(new Uri(options.AzureKeyVault.VaultUri), new DefaultAzureCredential());
        }

        if (options?.HashiCorpVault?.IsEnabled ?? false)
        {
            configurationBuilder.AddHashiCorpVault(options.HashiCorpVault);
        }

        return configurationBuilder;
    }

    public static IConfigurationBuilder AddAwsSystemsManager(this IConfigurationBuilder configurationBuilder, AwsSystemsManagerOptions options)
    {
        configurationBuilder.AddSystemsManager(options.ParameterPath, new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Credentials = new BasicAWSCredentials(options.AccessKeyID, options.SecretAccessKey),
            Region = RegionEndpoint.GetBySystemName(options.RegionEndpoint)
        });

        return configurationBuilder;
    }
}
