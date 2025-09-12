using DddDotNet.Infrastructure.Storages.Amazon;
using DddDotNet.Infrastructure.Storages.Azure;
using DddDotNet.Infrastructure.Storages.Local;

namespace DddDotNet.Infrastructure.Storages;

public class StorageOptions
{
    public string Provider { get; set; }

    public LocalOptions Local { get; set; }

    public AzureBlobOptions Azure { get; set; }

    public AmazonOptions Amazon { get; set; }

    public bool UsedLocal()
    {
        return Provider == "Local";
    }

    public bool UsedAzure()
    {
        return Provider == "Azure";
    }

    public bool UsedAmazon()
    {
        return Provider == "Amazon";
    }

    public bool UsedFake()
    {
        return Provider == "Fake";
    }
}
