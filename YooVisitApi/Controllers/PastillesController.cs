using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos;
using YooVisitApi.Models;

namespace YooVisitApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PastillesController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public PastillesController(ApiDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePastille([FromForm] PastilleCreateDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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
            };
            _context.Pastilles.Add(pastille);
            await _context.SaveChangesAsync();

            var uploadsFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "storage");
            var fileName = $"{pastille.Id}_{DateTime.UtcNow.Ticks}.jpg";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }

            var photo = new Photo
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                PastilleId = pastille.Id
            };
            _context.Photos.Add(photo);

            // On trouve l'utilisateur et on lui donne de l'XP
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Experience += 50; // 50 XP par pastille créée
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var createdPastille = await _context.Pastilles
                .Include(p => p.User)
                .Include(p => p.Photos)
                .FirstAsync(p => p.Id == pastille.Id);

            return Ok(MapToDto(createdPastille));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Erreur interne du serveur : {ex.Message}");
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PastilleDto>>> GetAllPastilles()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdString, out Guid currentUserId);

        var dtos = await _context.Pastilles
            .Include(p => p.User)
            .Include(p => p.Photos)
            .Include(p => p.Ratings)
            .AsNoTracking()
            .Select(p => new PastilleDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                CreatedByUserId = p.CreatedByUserId,
                CreatedByUserName = p.User.Nom,
                AverageRating = p.Ratings.Any() ? p.Ratings.Average(r => r.RatingValue) : 0,
                IsOwner = p.CreatedByUserId == currentUserId,

                Photos = p.Photos.Select(photo => new PhotoDto
                {
                    Id = photo.Id,
                    ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{photo.FileName}",
                    UploadedAt = photo.UploadedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(dtos);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePastille(Guid id, [FromBody] PastilleUpdateDto updateDto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastille = await _context.Pastilles.FindAsync(id);

        if (pastille == null) return NotFound("Pastille non trouvée.");

        // CONTRÔLE DE SÉCURITÉ : Seul le propriétaire peut modifier.
        if (pastille.CreatedByUserId != userId)
        {
            return Forbid("Vous n'avez pas l'autorisation de modifier cette pastille.");
        }

        pastille.Title = updateDto.Title;
        pastille.Description = updateDto.Description;
        await _context.SaveChangesAsync();

        return NoContent(); // Succès
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePastille(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastille = await _context.Pastilles.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == id);

        if (pastille == null) return NotFound("Pastille non trouvée.");

        // CONTRÔLE DE SÉCURITÉ : Seul le propriétaire peut supprimer.
        if (pastille.CreatedByUserId != userId)
        {
            return Forbid("Vous n'avez pas l'autorisation de supprimer cette pastille.");
        }

        // On supprime les fichiers physiques associés
        foreach (var photo in pastille.Photos)
        {
            var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "storage", photo.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // La suppression de la pastille entraînera la suppression en cascade des photos
        // et des ratings associés si la base de données est bien configurée.
        _context.Pastilles.Remove(pastille);
        await _context.SaveChangesAsync();

        return NoContent(); // Succès
    }

    [Authorize]
    [HttpPost("{id}/rate")]
    public async Task<IActionResult> RatePastille(Guid id, [FromBody] PastilleRatingDto request)
    {
        var raterUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastilleToRate = await _context.Pastilles.FindAsync(id);

        if (pastilleToRate == null) return NotFound("Pastille non trouvée.");
        if (pastilleToRate.CreatedByUserId == raterUserId) return BadRequest("Vous ne pouvez pas noter votre propre pastille.");

        var existingRating = await _context.PastilleRatings
            .FirstOrDefaultAsync(r => r.PastilleId == id && r.RaterUserId == raterUserId);

        if (existingRating != null) return Conflict("Vous avez déjà noté cette pastille.");

        _context.PastilleRatings.Add(new PastilleRating
        {
            Id = Guid.NewGuid(),
            PastilleId = id,
            RaterUserId = raterUserId,
            RatingValue = request.Rating,
            RatedAt = DateTime.UtcNow
        });

        var photoOwner = await _context.Users.FindAsync(pastilleToRate.CreatedByUserId);

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Merci pour votre vote !"});
    }
    private PastilleDto MapToDto(Pastille pastille)
    {
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
            CreatedByUserId = pastille.CreatedByUserId,

            // On va chercher le nom dans l'objet User qui a été joint à la requête
            CreatedByUserName = pastille.User?.Nom ?? pastille.User?.Email.Split('@').First() ?? "Inconnu",

            // On calcule la note moyenne. S'il n'y a pas de notes, on met 0.
            AverageRating = pastille.Ratings.Any() ? pastille.Ratings.Average(r => r.RatingValue) : 0,

            // On transforme la liste des entités Photo en une liste de PhotoDto
            Photos = pastille.Photos.Select(photo => new PhotoDto
            {
                Id = photo.Id,
                // On construit l'URL complète de l'image
                ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{photo.FileName}",
                UploadedAt = photo.UploadedAt
            }).ToList()
        };
    }
}
