using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class FileUploadRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "At least one file is required")]
        public required List<IFormFile> Files { get; set; }

        public required string ClientName { get; set; }

    }
}