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

        public FileUploadService(ILogger<FileUploadService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Get upload directory from configuration or use default
            _uploadDirectory = _configuration["FileUpload:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            // Ensure directory exists
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<bool> ProcessFileUploadAsync(FileUploadRequest request)
        {
            try
            {
                // Log the location if it exists
                if (!string.IsNullOrEmpty(request.ClientName))
                {
                    _logger.LogInformation($"Processing upload for location: {request.ClientName}");
                }

                // Create directory path based on location (if provided)
                string baseDir = _uploadDirectory;
                if (!string.IsNullOrEmpty(request.ClientName))
                {
                    baseDir = Path.Combine(_uploadDirectory, request.ClientName);
                    if (!Directory.Exists(baseDir))
                    {
                        Directory.CreateDirectory(baseDir);
                    }
                }

                // Create a unique subdirectory for this upload (using timestamp)
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var userDirectory = Path.Combine(baseDir, request.Email.Replace("@", "_at_"), timestamp);

                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                // Save each file to the directory
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

                // Here you would add your own logic for further processing
                // such as storing metadata in a database, triggering workflows, etc.

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