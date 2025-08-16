using Renci.SshNet;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Sftp;

public class SftpStorageManager : IFileStorageManager
{
    private readonly SftpOptions _options;

    public SftpStorageManager(SftpOptions options)
    {
        _options = options;
    }

    public Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to archive storage
        return Task.CompletedTask;
    }

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        using var sftpClient = GetSftpClient();
        sftpClient.Connect();

        var path = GetRemoteFilePath(fileEntry);

        CreateDirectories(sftpClient, path);

        sftpClient.UploadFile(stream, path, canOverride: true);
        await Task.CompletedTask;
    }

    private SftpClient GetSftpClient()
    {
        var connectionInfo = new ConnectionInfo(_options.Host,
            _options.UserName,
            new PasswordAuthenticationMethod(_options.UserName, _options.Password));
        var client = new SftpClient(connectionInfo);
        return client;
    }

    private string GetRemoteFilePath(IFileEntry fileEntry)
    {
        return UriPath.Combine(_options.Path, fileEntry.FileLocation);
    }

    private static void CreateDirectories(SftpClient sftpClient, string path)
    {
        var directory = Path.GetDirectoryName(path).Replace("\\", "/");

        string[] arrayPath = directory.Split('/');

        directory = string.Empty;
        for (int i = 0; i < arrayPath.Length; i++)
        {
            directory += arrayPath[i];

            if (!sftpClient.Exists(directory))
            {
                try
                {
                    sftpClient.CreateDirectory(directory);
                }
                catch
                {
                    if (!sftpClient.Exists(directory))
                    {
                        throw;
                    }
                }
            }

            directory += "/";
        }
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var sftpClient = GetSftpClient();
        sftpClient.Connect();
        sftpClient.DeleteFile(GetRemoteFilePath(fileEntry));
        await Task.CompletedTask;
    }

    public void Dispose()
    {
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
        using var sftpClient = GetSftpClient();
        sftpClient.Connect();
        sftpClient.DownloadFile(GetRemoteFilePath(fileEntry), stream);
        await Task.CompletedTask;
    }

    public Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to active storage
        return Task.CompletedTask;
    }
}
