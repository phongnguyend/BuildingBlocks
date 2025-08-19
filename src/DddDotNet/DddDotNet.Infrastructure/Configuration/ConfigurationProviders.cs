namespace DddDotNet.Infrastructure.Configuration;

public class ConfigurationProviders
{
    public SqlServerOptions SqlServer { get; set; }

    public AzureAppConfigurationOptions AzureAppConfiguration { get; set; }

    public AzureKeyVaultOptions AzureKeyVault { get; set; }

    public HashiCorpVaultOptions HashiCorpVault { get; set; }
}

public class AzureKeyVaultOptions
{
    public bool IsEnabled { get; set; }

    public string VaultUri { get; set; }
}

public class AzureAppConfigurationOptions
{
    public bool IsEnabled { get; set; }

    public string ConnectionString { get; set; }

    public string Endpoint { get; set; }
}

public class AwsSystemsManagerOptions
{
    public string AccessKeyID { get; set; }

    public string SecretAccessKey { get; set; }

    public string ParameterPath { get; set; }

    public string RegionEndpoint { get; set; }
}
