using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace YooVisitAPI.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    }
}
