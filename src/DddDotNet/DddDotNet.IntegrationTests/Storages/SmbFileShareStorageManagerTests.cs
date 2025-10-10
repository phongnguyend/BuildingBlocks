using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.Smb;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class SmbFileShareStorageManagerTests
{
    SmbFileShareOptions _options = new SmbFileShareOptions();

    public SmbFileShareStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:SmbFileShare").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        using var smbFileShareStorageManager = new SmbFileShareStorageManager(_options);

        var fileEntry = new FileEntry
        {
            FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await smbFileShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await smbFileShareStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await smbFileShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await smbFileShareStorageManager.ReadAsync(fileEntry));

        await smbFileShareStorageManager.ArchiveAsync(fileEntry);

        await smbFileShareStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await smbFileShareStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await smbFileShareStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await smbFileShareStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }
}
