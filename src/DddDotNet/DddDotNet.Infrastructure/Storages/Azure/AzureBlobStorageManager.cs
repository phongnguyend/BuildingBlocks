using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureBlobStorageManager : IFileStorageManager
{
    private readonly AzureBlobOption _option;
    private readonly BlobContainerClient _container;

    public AzureBlobStorageManager(AzureBlobOption option)
    {
        _option = option;
        _container = _option.CreateBlobContainerClient();
    }

    private string GetBlobName(IFileEntry fileEntry)
    {
        return UriPath.Combine(_option.Path, fileEntry.FileLocation);
    }

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.DeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await DownloadAsync(fileEntry, stream, cancellationToken);
        return stream.ToArray();
    }

    public async Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.DownloadToAsync(path, cancellationToken);
    }

    public async Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.DownloadToAsync(stream, cancellationToken);
    }

    public async Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.SetAccessTierAsync(AccessTier.Cool, cancellationToken: cancellationToken);
    }

    public async Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        BlobClient blob = _container.GetBlobClient(GetBlobName(fileEntry));
        await blob.SetAccessTierAsync(AccessTier.Hot, cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
    }
}
