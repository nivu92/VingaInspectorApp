using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Server.Interfaces;
using Server.Models;

namespace Server.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _uploadDirectory;
        private readonly IMinioService _minioService;

        public FileUploadService(
            ILogger<FileUploadService> logger,
            IConfiguration configuration,
            IMinioService minioService)
        {
            _logger = logger;
            _configuration = configuration;
            _minioService = minioService;

            // Optional local file saving logic:
            _uploadDirectory = _configuration["FileUpload:UploadDirectory"]
                               ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<bool> ProcessFileUploadAsync(FileUploadRequest request)
        {
            try
            {
                // ---- Existing logic (saving locally, logs, etc.) ----
                if (!string.IsNullOrEmpty(request.ClientName))
                {
                    _logger.LogInformation($"Processing upload for location: {request.ClientName}");
                }

                // (Optional) Save locally as you do now. 
                // Or you can skip the local write entirely if you only want MinIO.

                // ---- [1] Save locally (OPTIONAL) ----
                string baseDir = _uploadDirectory;
                if (!string.IsNullOrEmpty(request.ClientName))
                {
                    baseDir = Path.Combine(_uploadDirectory, request.ClientName);
                    if (!Directory.Exists(baseDir))
                    {
                        Directory.CreateDirectory(baseDir);
                    }
                }

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var userDirectory = Path.Combine(baseDir, request.Email.Replace("@", "_at_"), timestamp);

                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                foreach (var file in request.Files)
                {
                    if (file.Length > 0)
                    {
                        var filePath = Path.Combine(userDirectory, file.FileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        _logger.LogInformation($"File {file.FileName} saved to {filePath}");
                    }
                }

                // ---- [2] Upload to MinIO ----
                await _minioService.UploadFilesToMinioAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                throw; // Let the controller handle the exception
            }
        }
    }
}
