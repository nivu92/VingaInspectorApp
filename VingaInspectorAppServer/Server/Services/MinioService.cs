using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel; // For BucketExistsArgs, MakeBucketArgs, PutObjectArgs, etc.
using Minio.DataModel.Args;
using Server.Interfaces;
using Server.Models;

namespace Server.Services
{
    public class MinioService : IMinioService
    {
        private readonly ILogger<MinioService> _logger;
        private readonly MinioClient _minioClient;
        private readonly string _defaultRegion;

        public MinioService(ILogger<MinioService> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Read MinIO settings from configuration, e.g. appsettings.json:
            // "Minio": {
            //    "Endpoint": "localhost:9000",
            //    "AccessKey": "minioadmin",
            //    "SecretKey": "minioadmin",
            //    "Region":    "us-east-1"
            // }
            var endpoint = configuration["Minio:Endpoint"];
            var accessKey = configuration["Minio:AccessKey"];
            var secretKey = configuration["Minio:SecretKey"];
            _defaultRegion = configuration["Minio:Region"] ?? "us-east-1";

            _minioClient = (MinioClient?)new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }

        public async Task UploadFilesToMinioAsync(FileUploadRequest request)
        {
            try
            {
                // 1) Make sure the bucket name (client name) is valid
                if (string.IsNullOrEmpty(request.ClientName))
                {
                    throw new ArgumentException("ClientName must be provided for bucket name.");
                }

                var bucketName = request.ClientName.ToLower();
                // Any normalization needed, do it here, e.g. remove invalid chars, etc.

                // Check if bucket exists
                bool found = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(bucketName));

                if (!found)
                {
                    // Create the bucket
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(bucketName).WithLocation(_defaultRegion));
                    _logger.LogInformation($"Bucket '{bucketName}' created.");
                }

                // 2) Create a "folder" (GUID) inside the bucket
                var folderGuid = Guid.NewGuid().ToString();
                _logger.LogInformation($"Created folder GUID: {folderGuid}");

                // 3) Upload each file
                foreach (var file in request.Files)
                {
                    if (file.Length == 0)
                        continue;

                    // The "folder" is really just a prefix in object storage
                    string objectName = $"{folderGuid}/{file.FileName}";

                    using var fileStream = file.OpenReadStream();

                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithStreamData(fileStream)
                        .WithObjectSize(file.Length)
                        .WithContentType(file.ContentType);

                    await _minioClient.PutObjectAsync(putObjectArgs);

                    _logger.LogInformation($"Uploaded file '{file.FileName}' to bucket '{bucketName}' as '{objectName}'.");
                }

                // 4) Create and upload meta.json
                var metadata = new
                {
                    Email = request.Email,
                    ClientName = request.ClientName,
                    DateOfCreation = DateTime.UtcNow
                };

                string metaJson = JsonSerializer.Serialize(metadata);
                byte[] metaBytes = Encoding.UTF8.GetBytes(metaJson);

                var metaObjectName = $"{folderGuid}/meta.json";
                using var metaStream = new MemoryStream(metaBytes);

                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(metaObjectName)
                    .WithStreamData(metaStream)
                    .WithObjectSize(metaStream.Length)
                    .WithContentType("application/json")
                );

                _logger.LogInformation($"Uploaded meta.json to bucket '{bucketName}' as '{metaObjectName}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Minio");
                throw; // Let the calling code handle the exception
            }
        }
    }
}
