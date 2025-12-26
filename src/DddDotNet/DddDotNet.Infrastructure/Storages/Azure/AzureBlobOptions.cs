using Azure.Identity;
using Azure.Storage.Blobs;
using System;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureBlobOptions
{
    public bool UseManagedIdentity { get; set; }

    public string ConnectionString { get; set; }

    public string StorageAccountName { get; set; }

    public string Container { get; set; }

    public string Path { get; set; }

    public BlobContainerClient CreateBlobContainerClient()
    {
        if (UseManagedIdentity)
        {
            var containerUri = new Uri($"https://{StorageAccountName}.blob.core.windows.net/{Container}");
            return new BlobContainerClient(containerUri, new DefaultAzureCredential());
        }

        return new BlobContainerClient(ConnectionString, Container);
    }
}