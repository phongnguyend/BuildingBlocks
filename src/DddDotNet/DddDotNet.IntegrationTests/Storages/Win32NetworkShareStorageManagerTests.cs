using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.WindowsNetworkShare;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class Win32NetworkShareStorageManagerTests
{
    Win32NetworkShareOptions _options = new Win32NetworkShareOptions();

    public Win32NetworkShareStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:WindowsNetworkShare").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        using var networkShareStorageManager = new Win32NetworkShareStorageManager(_options);

        networkShareStorageManager.ResetConnection();

        var fileEntry = new FileEntry
        {
            FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await networkShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await networkShareStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await networkShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await networkShareStorageManager.ReadAsync(fileEntry));

        await networkShareStorageManager.ArchiveAsync(fileEntry);

        await networkShareStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await networkShareStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await networkShareStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await networkShareStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }
}
