using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace DddDotNet.Infrastructure.Storages.Google;

public class GoogleCloudStorageManager : IFileStorageManager
{
    private readonly GoogleCloudStorageOptions _options;
    private readonly StorageClient _storageClient;

    public GoogleCloudStorageManager(GoogleCloudStorageOptions options)
    {
        _options = options;

        GoogleCredential credential = null;
        using (var jsonStream = new FileStream(_options.CredentialFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            credential = GoogleCredential.FromStream(jsonStream);
        }

        _storageClient = StorageClient.Create(credential);
    }

    private string GetObjectName(IFileEntry fileEntry)
    {
        return UriPath.Combine(_options.Path, fileEntry.FileLocation);
    }

    public async Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var obj = new Object { StorageClass = "ARCHIVE" };
        var response = await _storageClient.Service.Objects.Rewrite(obj, _options.BucketName, GetObjectName(fileEntry), _options.BucketName, GetObjectName(fileEntry)).ExecuteAsync();
    }

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        var response = await _storageClient.UploadObjectAsync(_options.BucketName, GetObjectName(fileEntry), null, stream);
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        await _storageClient.DeleteObjectAsync(_options.BucketName, GetObjectName(fileEntry));
    }

    public void Dispose()
    {
    }

    public async Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var response = await _storageClient.DownloadObjectAsync(_options.BucketName, GetObjectName(fileEntry), stream);
    }

    public async Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        var response = await _storageClient.DownloadObjectAsync(_options.BucketName, GetObjectName(fileEntry), stream);
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        var response = await _storageClient.DownloadObjectAsync(_options.BucketName, GetObjectName(fileEntry), stream);
        return stream.ToArray();
    }

    public async Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var obj = new Object { StorageClass = "STANDARD" };
        var response = await _storageClient.Service.Objects.Rewrite(obj, _options.BucketName, GetObjectName(fileEntry), _options.BucketName, GetObjectName(fileEntry)).ExecuteAsync();
    }
}
