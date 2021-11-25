﻿using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Storages.Amazon
{
    public class AmazonS3StorageManager : IFileStorageManager
    {
        private readonly IAmazonS3 _client;
        private readonly AmazonOptions _options;

        public AmazonS3StorageManager(AmazonOptions options)
        {
            _client = new AmazonS3Client(options.AccessKeyID, options.SecretAccessKey, RegionEndpoint.GetBySystemName(options.RegionEndpoint));
            _options = options;
        }

        private string GetKey(IFileEntry fileEntry)
        {
            return _options.Path + fileEntry.FileLocation;
        }

        public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
        {
            var fileTransferUtility = new TransferUtility(_client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = GetKey(fileEntry),
                BucketName = _options.BucketName,
                CannedACL = S3CannedACL.NoACL,
            };

            await fileTransferUtility.UploadAsync(uploadRequest, cancellationToken);
        }

        public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            await _client.DeleteObjectAsync(_options.BucketName, GetKey(fileEntry), cancellationToken);
        }

        public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await DownloadAsync(fileEntry, stream, cancellationToken);
            return stream.ToArray();
        }

        public async Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
        {
            var request = new TransferUtilityDownloadRequest
            {
                BucketName = _options.BucketName,
                Key = GetKey(fileEntry),
                FilePath = path,
            };

            var fileTransferUtility = new TransferUtility(_client);
            await fileTransferUtility.DownloadAsync(request, cancellationToken);
        }

        public async Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = GetKey(fileEntry),
            };

            using var response = await _client.GetObjectAsync(request, cancellationToken);
            using var responseStream = response.ResponseStream;
            await responseStream.CopyToAsync(stream, cancellationToken);
        }

        public async Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            var copy = new CopyObjectRequest
            {
                SourceBucket = _options.BucketName,
                SourceKey = GetKey(fileEntry),
                DestinationBucket = _options.BucketName,
                DestinationKey = GetKey(fileEntry),
                StorageClass = S3StorageClass.StandardInfrequentAccess,
            };

            await _client.CopyObjectAsync(copy, cancellationToken);
        }

        public async Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            var copy = new CopyObjectRequest
            {
                SourceBucket = _options.BucketName,
                SourceKey = GetKey(fileEntry),
                DestinationBucket = _options.BucketName,
                DestinationKey = GetKey(fileEntry),
                StorageClass = S3StorageClass.Standard,
            };

            await _client.CopyObjectAsync(copy, cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
