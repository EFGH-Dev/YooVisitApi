using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YooVisitAPI.Interfaces;

namespace YooVisitAPI.Services
{

    public class FileStorageService : IFileStorageService
    {
        private readonly string _storagePath = "/app/storage";

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
        {
            // LA CORRECTION DÉFINITIVE : On autorise le type générique que l'on a découvert.
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "application/octet-stream" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                Console.WriteLine($"[VALIDATION ÉCHOUÉE] Type de fichier reçu: {file.ContentType}");
                throw new ArgumentException("Type de fichier non autorisé.");
            }

            var targetDirectory = Path.Combine(_storagePath, subDirectory);
            // Le code s'assure que le dossier existe.
            Directory.CreateDirectory(targetDirectory);

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(targetDirectory, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine($"Fichier sauvegardé avec succès : {fullPath}");

            return uniqueFileName;
        }
    }
}
