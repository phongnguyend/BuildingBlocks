﻿using DddDotNet.Infrastructure.Storages.Sftp;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Storages
{
    public class SfptStorageManagerTests
    {
        SftpOptions _options = new SftpOptions();

        public SfptStorageManagerTests()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
                .Build();

            config.GetSection("Storage:Sftp").Bind(_options);
        }

        [Fact]
        public async Task CreateAsync_Success()
        {
            using SftpStorageManager sftpStorageManager = new SftpStorageManager(_options);

            var fileEntry = new FileEntry
            {
                FileLocation = DateTime.Now.ToString("yyyy/MM/dd") + "/" + Guid.NewGuid()
            };

            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

            await sftpStorageManager.CreateAsync(fileEntry, fileStream);

            var content1 = Encoding.UTF8.GetString(await sftpStorageManager.ReadAsync(fileEntry));

            fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

            await sftpStorageManager.CreateAsync(fileEntry, fileStream);

            var content2 = Encoding.UTF8.GetString(await sftpStorageManager.ReadAsync(fileEntry));

            await sftpStorageManager.ArchiveAsync(fileEntry);

            await sftpStorageManager.UnArchiveAsync(fileEntry);

            var path = Path.GetTempFileName();
            await sftpStorageManager.DownloadAsync(fileEntry, path);
            var content3 = File.ReadAllText(path);
            File.Delete(path);

            path = Path.GetTempFileName();
            using (var tempFileStream = File.OpenWrite(path))
            {
                await sftpStorageManager.DownloadAsync(fileEntry, tempFileStream);
            }
            var content4 = File.ReadAllText(path);
            File.Delete(path);

            await sftpStorageManager.DeleteAsync(fileEntry);

            Assert.Equal("Test", content1);
            Assert.Equal("Test2", content2);
            Assert.Equal("Test2", content3);
            Assert.Equal("Test2", content4);
        }
    }
}
