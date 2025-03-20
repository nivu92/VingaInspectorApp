
using Server.Models;

namespace Server.Interfaces
{
    public interface IFileUploadService
    {
        Task<bool> ProcessFileUploadAsync(FileUploadRequest request);
    }
}