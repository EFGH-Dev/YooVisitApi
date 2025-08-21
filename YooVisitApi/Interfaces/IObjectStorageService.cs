namespace YooVisitApi.Services
{
    public interface IObjectStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileKey);
        string GeneratePresignedUploadUrl(string fileName, string contentType);
        string GeneratePresignedGetUrl(string fileKey);
        Task RenameFileAsync(string oldKey, string newKey);
    }
}