using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Smb;

public class SmbFileShareStorageManager : IFileStorageManager
{
    private readonly object _lock = new object();

    private readonly SmbFileShareOptions _options;
    private SMB2Client _smbClient;
    private ISMBFileStore _fileStore;

    public SmbFileShareStorageManager(SmbFileShareOptions options)
    {
        _options = options;
    }

    private void Init()
    {
        if (_fileStore != null)
        {
            return;
        }

        lock (_lock)
        {
            if (_fileStore != null)
            {
                return;
            }

            _smbClient = new SMB2Client();
            var connected = _smbClient.Connect(_options.HostName, SMBTransportType.DirectTCPTransport);
            var loggedIn = _smbClient.Login(_options.DomainName, _options.UserName, _options.Password);

            NTStatus status;
            _fileStore = _smbClient.TreeConnect(_options.ShareName, out status);
        }
    }

    public Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to archive storage
        return Task.CompletedTask;
    }

    private string GetRemoteFilePath(IFileEntry fileEntry)
    {
        return UriPath.Combine(_options.Path, fileEntry.FileLocation);
    }

    public Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        Init();

        NTStatus status;
        object fileHandle;
        FileStatus fileStatus;

        CreateDirectory(fileEntry);

        status = _fileStore.CreateFile(out fileHandle, out fileStatus, GetRemoteFilePath(fileEntry), AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OVERWRITE_IF, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to create file. Status: {status}");
        }

        int writeOffset = 0;
        while (stream.Position < stream.Length)
        {
            byte[] buffer = new byte[(int)_smbClient.MaxWriteSize];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead < (int)_smbClient.MaxWriteSize)
            {
                Array.Resize<byte>(ref buffer, bytesRead);
            }

            int numberOfBytesWritten;
            status = _fileStore.WriteFile(out numberOfBytesWritten, fileHandle, writeOffset, buffer);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new Exception($"Failed to write to file. Status: {status}");
            }

            writeOffset += bytesRead;
        }

        status = _fileStore.CloseFile(fileHandle);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to close file. Status: {status}");
        }

        return Task.CompletedTask;
    }

    private void CreateDirectory(IFileEntry fileEntry)
    {
        NTStatus status;
        object fileHandle;
        FileStatus fileStatus;

        var directory = Path.GetDirectoryName(GetRemoteFilePath(fileEntry)).Replace("\\", "/");

        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        status = _fileStore.CreateFile(out fileHandle, out fileStatus, directory, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE, null);
        if (status == NTStatus.STATUS_OBJECT_NAME_COLLISION)
        {
            return;
        }

        string[] arrayPath = directory.Split('/');

        directory = string.Empty;
        for (int i = 0; i < arrayPath.Length; i++)
        {
            directory += arrayPath[i];
            status = _fileStore.CreateFile(out fileHandle, out fileStatus, directory, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE, null);

            if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_OBJECT_NAME_COLLISION)
            {
                throw new Exception($"Failed to create directory. Status: {status}");
            }

            directory += "/";
        }
    }

    public Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        Init();

        NTStatus status;
        object fileHandle;
        FileStatus fileStatus;

        status = _fileStore.CreateFile(out fileHandle, out fileStatus, GetRemoteFilePath(fileEntry), AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to create file. Status: {status}");
        }

        FileDispositionInformation fileDispositionInformation = new FileDispositionInformation
        {
            DeletePending = true,
        };
        status = _fileStore.SetFileInformation(fileHandle, fileDispositionInformation);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to delete file. Status: {status}");
        }

        status = _fileStore.CloseFile(fileHandle);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to close file. Status: {status}");
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await DownloadAsync(fileEntry, stream, cancellationToken);
        return stream.ToArray();
    }

    public Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        return DownloadAsync(fileEntry, stream, cancellationToken);
    }

    public Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        Init();

        NTStatus status;
        object fileHandle;
        FileStatus fileStatus;

        status = _fileStore.CreateFile(out fileHandle, out fileStatus, GetRemoteFilePath(fileEntry), AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to open file. Status: {status}");
        }

        byte[] data;
        long bytesRead = 0;
        while (true)
        {
            status = _fileStore.ReadFile(out data, fileHandle, bytesRead, (int)_smbClient.MaxReadSize);

            if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
            {
                throw new Exception($"Failed to read from file. Status: {status}");
            }

            if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
            {
                break;
            }

            bytesRead += data.Length;
            stream.Write(data, 0, data.Length);
        }

        status = _fileStore.CloseFile(fileHandle);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception($"Failed to close file. Status: {status}");
        }

        return Task.CompletedTask;
    }

    public Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // TODO: move to active storage
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        NTStatus status;
        if (_fileStore != null)
        {
            status = _fileStore.Disconnect();
        }

        if (_smbClient != null)
        {
            status = _smbClient.Logoff();
            _smbClient.Disconnect();
        }
    }
}
