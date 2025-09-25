using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System;

namespace DddDotNet.Infrastructure.Caching;

public class CachingOptions
{
    public InMemoryCacheOptions InMemory { get; set; }

    public DistributedCacheOptions Distributed { get; set; }
}

public class InMemoryCacheOptions
{
    public long? SizeLimit { get; set; }
}

public class DistributedCacheOptions
{
    public string Provider { get; set; }

    public InMemoryCacheOptions InMemory { get; set; }

    public RedisOptions Redis { get; set; }

    public SqlServerOptions SqlServer { get; set; }

    public CosmosOptions Cosmos { get; set; }
}

public class RedisOptions
{
    public string Configuration { get; set; }

    public string InstanceName { get; set; }
}

public class SqlServerOptions
{
    public string ConnectionString { get; set; }

    public string SchemaName { get; set; }

    public string TableName { get; set; }
}

public class CosmosOptions
{
    public string ConnectionString { get; set; }

    public string AccountEndpoint { get; set; }

    public string DatabaseName { get; set; }

    public string ContainerName { get; set; }

    public CosmosClientOptions CosmosClientOptions { get; set; }

    public CosmosClient CreateCosmosClient()
    {
        var options = GetCosmosClientOptions();

        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return options == null ?
                new CosmosClient(ConnectionString) :
                new CosmosClient(ConnectionString, options);
        }
        else if (!string.IsNullOrWhiteSpace(AccountEndpoint))
        {
            return options == null ?
                new CosmosClient(AccountEndpoint, new DefaultAzureCredential()) :
                new CosmosClient(AccountEndpoint, new DefaultAzureCredential(), options);
        }
        else
        {
            throw new InvalidOperationException("Either ConnectionString or AccountEndpoint must be provided.");
        }
    }

    private CosmosClientOptions GetCosmosClientOptions()
    {
        if (CosmosClientOptions == null)
        {
            return null;
        }

        return new CosmosClientOptions
        {
            ConnectionMode = CosmosClientOptions.ConnectionMode,
        };
    }
}
