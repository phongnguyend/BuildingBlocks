using Azure.Identity;
using Azure.Storage.Files.Shares;
using System;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureFileShareOptions
{
    public bool UseManagedIdentity { get; set; }

    public string ConnectionString { get; set; }

    public string ShareName { get; set; }

    public string Path { get; set; }

    public ShareClient CreateShareClient()
    {
        if (UseManagedIdentity)
        {
            var shareUri = new Uri($"https://{ShareName}.file.core.windows.net/{ShareName}");
            return new ShareClient(shareUri, new DefaultAzureCredential());
        }

        return new ShareClient(ConnectionString, ShareName);
    }
}
