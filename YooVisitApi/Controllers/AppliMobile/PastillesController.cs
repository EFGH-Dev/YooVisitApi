using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Pastille;
using YooVisitApi.Dtos.Photo;
using YooVisitApi.Dtos.Storage;
using YooVisitApi.Models.PastilleModel;
using YooVisitApi.Models.PhotoModel;
using Microsoft.Extensions.Logging;
using YooVisitApi.Services; // AJOUTÉ : Le using pour notre nouveau service
using Microsoft.AspNetCore.SignalR;
using YooVisitApi.Hubs;

namespace YooVisitApi.Controllers.AppliMobile
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PastillesController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IObjectStorageService _storageService; // On injecte notre service
        private readonly IConfiguration _configuration; // Pour lire la config (URL de base S3)
        private readonly ILogger<PastillesController> _logger; // Pour le logging
        private readonly IHubContext<Updates> _hubContext; // Pour SignalR

        // Le constructeur est mis à jour pour injecter les nouveaux services
        public PastillesController(ApiDbContext context, IObjectStorageService storageService, IConfiguration configuration, ILogger<PastillesController> logger, IHubContext<Updates> hubContext)
        {
            _context = context;
            _storageService = storageService;
            _configuration = configuration;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("generate-upload-url")]
        public IActionResult GenerateUploadUrl([FromBody] UploadUrlRequestDto requestDto)
        {
            // On vérifie que le nom de fichier envoyé par Flutter est bien un GUID
            // pour des raisons de sécurité et de cohérence.
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(requestDto.FileName), out _))
            {
                return BadRequest("Le nom de fichier doit être un GUID.jpg valide.");
            }

            // ON UTILISE LE NOM DE FICHIER FOURNI PAR LE CLIENT
            var fileKey = requestDto.FileName;

            var presignedUrl = _storageService.GeneratePresignedUploadUrl(fileKey, requestDto.ContentType);

            // On renvoie le même fileKey pour confirmation
            return Ok(new { uploadUrl = presignedUrl, fileKey });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePastille([FromBody] PastilleCreateDto request)
        {
            if (string.IsNullOrEmpty(request.FileKey))
            {
                return BadRequest("Le champ FileKey est requis.");
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Création de la pastille
                var pastille = new Pastille
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Altitude = request.Altitude,
                    StyleArchitectural = request.StyleArchitectural,
                    PeriodeConstruction = request.PeriodeConstruction,
                    HorairesOuverture = request.HorairesOuverture,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Pastilles.Add(pastille);

                // Création de la photo
                var photo = new Photo
                {
                    Id = Guid.NewGuid(),
                    FileName = Path.GetFileName(request.FileKey),
                    FileKey = request.FileKey,
                    UploadedAt = DateTime.UtcNow,
                    PastilleId = pastille.Id
                };
                _context.Photos.Add(photo);

                // Mise à jour de l'expérience utilisateur
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Experience += 50;
                }

                // Sauvegarde et commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Appel de BroadcastPastilleUpdateAsync pour la création...");
                await BroadcastPastilleUpdateAsync(pastille.Id, "Created");

                var createdPastille = await _context.Pastilles
                    .Include(p => p.User)
                    .Include(p => p.Photos)
                    .FirstAsync(p => p.Id == pastille.Id);

                return Ok(MapToDto(createdPastille));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Erreur interne du serveur: {ex.Message}");
            }
        }

        [HttpPut("{pastilleId}/photo")]
        public async Task<IActionResult> UpdatePastillePhoto([FromRoute] Guid pastilleId, [FromBody] UpdatePhotoDto request)
        {
            // 1. Sécurité : Vérifier que l'utilisateur est bien le propriétaire
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pastille = await _context.Pastilles
                .Include(p => p.Photos) // Important de charger les photos existantes !
                .FirstOrDefaultAsync(p => p.Id == pastilleId);

            if (pastille == null)
            {
                return NotFound("Pastille non trouvée.");
            }

            if (pastille.CreatedByUserId != userId)
            {
                return Forbid("Action non autorisée. Vous n'êtes pas le propriétaire de cette pastille.");
            }

            // 2. Nettoyage : Supprimer l'ancienne photo du stockage S3
            var oldPhoto = pastille.Photos.FirstOrDefault();
            if (oldPhoto != null && !string.IsNullOrEmpty(oldPhoto.FileKey))
            {
                // On demande au service de supprimer l'ancien fichier
                await _storageService.DeleteFileAsync(oldPhoto.FileKey);
            }

            // 3. Mise à jour (ou création) en base de données
            if (oldPhoto != null)
            {
                // La pastille avait déjà une photo, on la met à jour
                oldPhoto.FileKey = request.FileKey;
                oldPhoto.FileName = Path.GetFileName(request.FileKey);
                oldPhoto.UploadedAt = DateTime.UtcNow;
            }
            else
            {
                // La pastille n'avait pas de photo, on en crée une nouvelle
                var newPhoto = new Photo
                {
                    Id = Guid.NewGuid(),
                    FileKey = request.FileKey,
                    FileName = Path.GetFileName(request.FileKey),
                    UploadedAt = DateTime.UtcNow,
                    PastilleId = pastilleId
                };
                _context.Photos.Add(newPhoto);
            }

            // 4. Sauvegarder les changements
            await _context.SaveChangesAsync();

            return Ok(new { message = "Photo mise à jour avec succès." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PastilleDto>>> GetAllPastilles()
        {
            string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdString, out Guid currentUserId);

            List<Pastille> pastillesFromDb = await _context.Pastilles
                .Include(p => p.User)
                .Include(p => p.Photos)
                .Include(p => p.Ratings)
                .AsNoTracking()
                .ToListAsync();

            List<PastilleDto> dtos = pastillesFromDb.Select(p => MapToDto(p, currentUserId)).ToList();

            return Ok(dtos);
        }

        [HttpGet("my-pastilles")]
        public async Task<ActionResult<IEnumerable<PastilleDto>>> GetMyPastilles()
        {
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            List<Pastille> pastillesFromDb = await _context.Pastilles
                .Where(p => p.CreatedByUserId == userId)
                .Include(p => p.User)
                .Include(p => p.Photos)
                .Include(p => p.Ratings)
                .AsNoTracking()
                .ToListAsync();

            List<PastilleDto> dtos = pastillesFromDb.Select(p => MapToDto(p, userId)).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<PastilleDto>> GetPastilleById(Guid id)
        {
            string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdString, out Guid currentUserId);

            Pastille? pastille = await _context.Pastilles
                .Include(p => p.User)
                .Include(p => p.Photos)
                .Include(p => p.Ratings)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pastille == null)
            {
                return NotFound("Pastille non trouvée.");
            }

            PastilleDto dto = MapToDto(pastille, currentUserId);

            return Ok(dto);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePastille(Guid id, [FromBody] PastilleUpdateDto updateDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pastille = await _context.Pastilles.FindAsync(id);
            if (pastille == null) return NotFound("Pastille non trouvée.");
            if (pastille.CreatedByUserId != userId) return Forbid("Vous n'avez pas l'autorisation de modifier cette pastille.");
            pastille.Title = updateDto.Title;
            pastille.Description = updateDto.Description;
            pastille.Latitude = updateDto.Latitude;
            pastille.Longitude = updateDto.Longitude;
            pastille.StyleArchitectural = updateDto.StyleArchitectural;
            pastille.PeriodeConstruction = updateDto.PeriodeConstruction;
            pastille.HorairesOuverture = updateDto.HorairesOuverture;
            await _context.SaveChangesAsync();
            // On broadcast la mise à jour à tous les clients
            await BroadcastPastilleUpdateAsync(id, "Updated");
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePastille(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pastille = await _context.Pastilles.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == id);

            if (pastille == null) return NotFound("Pastille non trouvée.");

            if (pastille.CreatedByUserId != userId)
            {
                return Forbid("Vous n'avez pas l'autorisation de supprimer cette pastille.");
            }

            // CHANGÉ : On supprime les fichiers du Stockage Objet avant de supprimer la pastille
            foreach (var photo in pastille.Photos)
            {
                await _storageService.DeleteFileAsync(photo.FileKey);
            }

            _context.Pastilles.Remove(pastille);
            await _context.SaveChangesAsync();

            // On broadcast la suppression à tous les clients
            // Pour la suppression, on envoie juste l'ID, c'est suffisant.
            var updateMessage = new { 
                EntityType = "Pastille", 
                Action = "Deleted", 
                Payload = new { Id = id } 
            };
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", updateMessage);

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RatePastille(Guid id, [FromBody] PastilleRatingDto request)
        {
            // ... (Aucun changement dans cette méthode)
            var raterUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pastilleToRate = await _context.Pastilles.FindAsync(id);
            if (pastilleToRate == null) return NotFound("Pastille non trouvée.");
            if (pastilleToRate.CreatedByUserId == raterUserId) return BadRequest("Vous ne pouvez pas noter votre propre pastille.");
            var existingRating = await _context.PastilleRatings.FirstOrDefaultAsync(r => r.PastilleId == id && r.RaterUserId == raterUserId);
            if (existingRating != null) return Conflict("Vous avez déjà noté cette pastille.");
            _context.PastilleRatings.Add(new PastilleRating { Id = Guid.NewGuid(), PastilleId = id, RaterUserId = raterUserId, RatingValue = request.Rating, RatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            await BroadcastPastilleUpdateAsync(id, "Updated");
            return Ok(new { Message = "Merci pour votre vote !" });
        }

        private PastilleDto MapToDto(Pastille pastille, Guid? currentUserId = null)
        {
            var firstPhoto = pastille.Photos?.FirstOrDefault(p => !string.IsNullOrEmpty(p.FileKey));

            var photoUrl = firstPhoto != null
                ? _storageService.GeneratePresignedGetUrl(firstPhoto.FileKey)
                : null;

            // The Console.WriteLine calls can be removed if you no longer need them for debugging
            Console.WriteLine($"--- Server UTC Time: {DateTime.UtcNow:O}");
            if (firstPhoto != null)
            {
                Console.WriteLine($"--- URL Générée pour {firstPhoto.FileKey}: {photoUrl} ---");
            }

            return new PastilleDto
            {
                Id = pastille.Id,
                Title = pastille.Title,
                Description = pastille.Description,
                Latitude = pastille.Latitude,
                Longitude = pastille.Longitude,
                Altitude = pastille.Altitude,
                StyleArchitectural = pastille.StyleArchitectural,
                PeriodeConstruction = pastille.PeriodeConstruction,
                HorairesOuverture = pastille.HorairesOuverture,

                // CORRECTION : Add this line to include the creator's ID
                CreatedByUserId = pastille.CreatedByUserId,

                CreatedByUserName = pastille.User?.Nom ?? pastille.User?.Email.Split('@').First() ?? "Inconnu",
                AverageRating = pastille.Ratings.Any() ? pastille.Ratings.Average(r => r.RatingValue) : 0,
                PhotoUrl = photoUrl,
                Photos = pastille.Photos
                    .Where(p => !string.IsNullOrEmpty(p.FileKey))
                    .Select(photo => new PhotoDto
                    {
                        Id = photo.Id,
                        ImageUrl = _storageService.GeneratePresignedGetUrl(photo.FileKey),
                        UploadedAt = photo.UploadedAt
                    }).ToList(),
            };
        }

        // Une méthode utilitaire pour centraliser la logique de broadcast
        private async Task BroadcastPastilleUpdateAsync(Guid pastilleId, string action)
        {
            _logger.LogInformation("--- INTERIEUR de BroadcastPastilleUpdateAsync pour l'action {Action} ---", action);
            try
            {
                var updatedPastille = await _context.Pastilles
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.Photos)
                    .Include(p => p.Ratings)
                    .FirstOrDefaultAsync(p => p.Id == pastilleId);

                if (updatedPastille != null)
                {
                    var pastilleDto = MapToDto(updatedPastille);
                    var updateMessage = new
                    {
                        EntityType = "Pastille",
                        Action = action,
                        Payload = pastilleDto
                    };
                    _logger.LogInformation(">>> Prêt à envoyer le broadcast SignalR pour {Action}", action);
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", updateMessage);
                    _logger.LogInformation("<<< Broadcast SignalR pour {Action} envoyé avec succès.", action);
                }
                else
                {
                    _logger.LogWarning($"Pastille {pastilleId} non trouvée pour le broadcast {action}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du broadcast de la pastille {pastilleId} ({action}).");
            }
        }
    }
}