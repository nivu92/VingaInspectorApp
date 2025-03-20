using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<FileUploadController> _logger;


        public FileUploadController(IFileUploadService fileUploadService, ILogger<FileUploadController> logger)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles([FromForm] FileUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Upload request received. Email: {Email}, Files Count: {Count}",
                    request.Email,
                    request.Files?.Count ?? 0);

                if (request.Files == null || request.Files.Count == 0)
                {
                    _logger.LogWarning("No files uploaded");
                    return BadRequest("No files uploaded");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    _logger.LogWarning("Email is required");
                    return BadRequest("Email is required");
                }

                // Log information about each file
                foreach (var file in request.Files)
                {
                    _logger.LogInformation("File: {FileName}, Type: {ContentType}, Size: {Length} bytes",
                        file.FileName,
                        file.ContentType,
                        file.Length);

                    // Validate all files are PDF
                    if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
                        !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("File '{FileName}' is not a PDF. Content type: {ContentType}",
                            file.FileName,
                            file.ContentType);
                        return BadRequest($"File '{file.FileName}' is not a PDF");
                    }
                }

                // Process the upload
                var result = await _fileUploadService.ProcessFileUploadAsync(request);

                _logger.LogInformation("Files uploaded successfully, count: {Count}", request.Files.Count);
                return Ok(new { Success = true, Message = "Files uploaded successfully", FilesCount = request.Files.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading files");
                return StatusCode(500, $"An error occurred while uploading files: {ex.Message}");
            }
        }
    }
}