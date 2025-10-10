using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.Azure;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class AzureFileShareStorageManagerTests
{
    AzureFileShareOptions _options = new AzureFileShareOptions();

    public AzureFileShareStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:AzureFileShare").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        var azureFileShareStorageManager = new AzureFileShareStorageManager(_options);

        var fileEntry = new FileEntry
        {
            FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await azureFileShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await azureFileShareStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await azureFileShareStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await azureFileShareStorageManager.ReadAsync(fileEntry));

        await azureFileShareStorageManager.ArchiveAsync(fileEntry);

        await azureFileShareStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await azureFileShareStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await azureFileShareStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await azureFileShareStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }
}
