using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class LocalFileStorageManagerTests
{
    LocalOptions _options = new LocalOptions();

    public LocalFileStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:Local").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        var localFileStorageManager = new LocalFileStorageManager(_options);

        var fileEntry = new FileEntry
        {
            FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await localFileStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await localFileStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await localFileStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await localFileStorageManager.ReadAsync(fileEntry));

        await localFileStorageManager.ArchiveAsync(fileEntry);

        await localFileStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await localFileStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await localFileStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await localFileStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }

    [Fact]
    public async Task HealthCheck_Success()
    {
        var healthCheck = new LocalFileHealthCheck(new LocalFileHealthCheckOptions
        {
            Path = _options.Path
        });
        var checkResult = await healthCheck.CheckHealthAsync(null);
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }
}
