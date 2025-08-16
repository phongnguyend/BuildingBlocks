using FluentFTP;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Ftp;

public class FtpStorageManager : IFileStorageManager
{
    private readonly FtpOptions _options;

    public FtpStorageManager(FtpOptions options)
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
        using var ftp = GetFtpClient();
        await ftp.Connect(cancellationToken);
        await ftp.UploadStream(stream, GetRemoteFilePath(fileEntry), FtpRemoteExists.Overwrite, true, token: cancellationToken);
    }

    private AsyncFtpClient GetFtpClient()
    {
        var ftpClient = new AsyncFtpClient(_options.Host, _options.UserName, _options.Password)
        {
            Config = new FtpConfig
            {
                EncryptionMode = FtpEncryptionMode.Implicit,

                // ValidateAnyCertificate = true,
            },
        };
        return ftpClient;
    }

    private string GetRemoteFilePath(IFileEntry fileEntry)
    {
        return UriPath.Combine(_options.Path, fileEntry.FileLocation);
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var ftp = GetFtpClient();
        await ftp.Connect(cancellationToken);
        await ftp.DeleteFile(GetRemoteFilePath(fileEntry), cancellationToken);
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
        using var ftp = GetFtpClient();
        await ftp.Connect(cancellationToken);
        using var fStream = await ftp.OpenRead(GetRemoteFilePath(fileEntry), token: cancellationToken);
        try
        {
            await fStream.CopyToAsync(stream, cancellationToken);
        }
        finally
        {
            fStream.Close();
        }
    }

    public Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to active storage
        return Task.CompletedTask;
    }
}
