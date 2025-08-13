using Azure.Identity;
using Azure.Storage.Blobs;
using System;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureBlobOption
{
    public string ConnectionString { get; set; }

    public string Container { get; set; }

    public string Path { get; set; }

    public BlobContainerClient CreateBlobContainerClient()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new BlobContainerClient(ConnectionString, Container);
        }
        else
        {
            var containerUri = new Uri($"https://{Container}.blob.core.windows.net");
            return new BlobContainerClient(containerUri, new DefaultAzureCredential());
        }
    }
}
