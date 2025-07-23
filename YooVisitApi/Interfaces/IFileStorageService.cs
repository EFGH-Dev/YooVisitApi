namespace YooVisitApi.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    }
}
