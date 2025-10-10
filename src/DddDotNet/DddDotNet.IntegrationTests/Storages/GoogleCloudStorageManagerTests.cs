using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class GoogleCloudStorageManagerTests
{
    GoogleCloudStorageOptions _options = new GoogleCloudStorageOptions();

    public GoogleCloudStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:Google").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        var googleCloudStorageManager = new GoogleCloudStorageManager(_options);

        var fileEntry = new FileEntry
        {
            FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await googleCloudStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await googleCloudStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await googleCloudStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await googleCloudStorageManager.ReadAsync(fileEntry));

        await googleCloudStorageManager.ArchiveAsync(fileEntry);

        await googleCloudStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await googleCloudStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await googleCloudStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await googleCloudStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }

    [Fact]
    public async Task HealthCheck_Success()
    {
        var healthCheck = new GoogleCloudStorageHealthCheck(_options);
        var checkResult = await healthCheck.CheckHealthAsync(null);
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }
}
