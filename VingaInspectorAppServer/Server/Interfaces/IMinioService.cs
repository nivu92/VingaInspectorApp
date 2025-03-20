using Server.Models;

namespace Server.Interfaces
{
    public interface IMinioService
    {
        Task UploadFilesToMinioAsync(FileUploadRequest request);
    }
}
