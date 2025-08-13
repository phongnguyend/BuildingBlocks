using Azure.Identity;
using Azure.Storage.Files.Shares;
using System;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureFileShareOptions
{
    public string ConnectionString { get; set; }

    public string ShareName { get; set; }

    public string Path { get; set; }

    public ShareClient CreateShareClient()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new ShareClient(ConnectionString, ShareName);
        }
        else
        {
            var shareUri = new Uri($"https://{ShareName}.file.core.windows.net/{ShareName}");
            return new ShareClient(shareUri, new DefaultAzureCredential());
        }
    }
}
