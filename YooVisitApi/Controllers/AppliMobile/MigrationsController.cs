using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Services;

namespace YooVisitApi.Controllers.AppliMobile
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin")] // <-- Sécurise cet endpoint !
    public class MigrationController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IObjectStorageService _storageService;

        public MigrationController(ApiDbContext context, IObjectStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // Un endpoint que tu appelleras UNE SEULE FOIS.
        [HttpPost("fix-photo-filekeys")]
        public async Task<IActionResult> FixPhotoFileKeys()
        {
            // 1. On récupère toutes les photos "cassées"
            var photosToFix = await _context.Photos
                .Where(p => string.IsNullOrEmpty(p.FileKey))
                .ToListAsync();

            if (!photosToFix.Any())
            {
                return Ok("Aucune photo à corriger !");
            }

            var report = new List<string>();

            foreach (var photo in photosToFix)
            {
                try
                {
                    // 2. On génère une nouvelle FileKey propre
                    var newFileKey = $"{Guid.NewGuid()}.jpg";
                    var oldFileKey = photo.FileName; // L'ancien nom est dans FileName

                    if (string.IsNullOrEmpty(oldFileKey))
                    {
                        report.Add($"ERREUR pour Photo ID {photo.Id}: FileName est vide, impossible de renommer.");
                        continue;
                    }

                    // 3. On renomme le fichier sur le stockage S3
                    // (Cette méthode est à ajouter dans ton service)
                    await _storageService.RenameFileAsync(oldFileKey, newFileKey);

                    // 4. On met à jour la base de données
                    photo.FileKey = newFileKey;
                    // Optionnel : on peut aussi nettoyer FileName
                    // photo.FileName = newFileKey;

                    report.Add($"SUCCÈS: Photo ID {photo.Id} | Ancien nom '{oldFileKey}' -> Nouveau nom '{newFileKey}'");
                }
                catch (Exception ex)
                {
                    report.Add($"ÉCHEC pour Photo ID {photo.Id}: {ex.Message}");
                }
            }

            // 5. On sauvegarde tous les changements en base de données en une seule fois
            await _context.SaveChangesAsync();

            return Ok(report);
        }
    }
}
