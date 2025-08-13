using Azure;
using Azure.Storage.Files.Shares;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureFileShareStorageManager : IFileStorageManager
{
    private readonly AzureFileShareOptions _options;
    private readonly ShareClient _shareClient;

    public AzureFileShareStorageManager(AzureFileShareOptions options)
    {
        _options = options;
        _shareClient = _options.CreateShareClient();
    }

    public Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to archive storage
        return Task.CompletedTask;
    }

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        ShareFileClient file = await GetShareFileClientAsync(fileEntry, true, cancellationToken);
        await file.CreateAsync(stream.Length, cancellationToken: cancellationToken);
        await file.UploadRangeAsync(new HttpRange(0, stream.Length), stream, cancellationToken: cancellationToken);
    }

    private async Task<ShareFileClient> GetShareFileClientAsync(IFileEntry fileEntry, bool createDirectories, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(fileEntry.FileLocation);
        string[] arrayPath = (_options.Path + fileEntry.FileLocation).Split('/');
        var directory = _shareClient.GetRootDirectoryClient();

        for (int i = 0; i < arrayPath.Length - 1; i++)
        {
            directory = directory.GetSubdirectoryClient(arrayPath[i]);
        }

        if (createDirectories)
        {
            var exists = await directory.ExistsAsync(cancellationToken: cancellationToken);
            if (!exists.Value)
            {
                directory = _shareClient.GetRootDirectoryClient();

                for (int i = 0; i < arrayPath.Length - 1; i++)
                {
                    directory = directory.GetSubdirectoryClient(arrayPath[i]);
                    await directory.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                }
            }
        }

        var file = directory.GetFileClient(fileName);
        return file;
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        ShareFileClient file = await GetShareFileClientAsync(fileEntry, false, cancellationToken);
        await file.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await DownloadAsync(fileEntry, stream, cancellationToken);
        return stream.ToArray();
    }

    public async Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await DownloadAsync(fileEntry, stream, cancellationToken);
    }

    public async Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        ShareFileClient file = await GetShareFileClientAsync(fileEntry, false, cancellationToken);
        var download = await file.DownloadAsync(cancellationToken: cancellationToken);
        await download.Value.Content.CopyToAsync(stream, cancellationToken);
    }

    public Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to active storage
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
