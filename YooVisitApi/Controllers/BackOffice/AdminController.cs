using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Dtos.User;

[ApiController]
[Route("api/[controller]")]
[ApiKeyAuthorize] // <--- MAGIE ! On protège TOUT le contrôleur avec notre clé.
public class AdminController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public AdminController(ApiDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    // Endpoint pour que le back-office puisse supprimer n'importe quelle pastille.
    [HttpDelete("pastilles/{id}")]
    public async Task<IActionResult> DeleteAnyPastille(Guid id)
    {
        var pastille = await _context.Pastilles.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == id);

        if (pastille == null)
        {
            return NotFound("Pastille non trouvée.");
        }

        // Note bien qu'on ne vérifie PAS le CreatedByUserId. C'est un pouvoir d'admin !

        // On supprime les fichiers physiques associés
        foreach (var photo in pastille.Photos)
        {
            var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "storage", photo.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.Pastilles.Remove(pastille);
        await _context.SaveChangesAsync();

        return NoContent(); // Succès
    }

    // Endpoint pour lister tous les utilisateurs de la plateforme.
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Select(user => new UserDto
            {
                IdUtilisateur = user.IdUtilisateur,
                Email = user.Email,
                Nom = user.Nom,
                Biographie = user.Biographie,
                Experience = user.Experience,
                DateInscription = user.DateInscription,
                ProfilePictureUrl = user.ProfilePictureFileName == null
                    ? null
                    : $"{Request.Scheme}://{Request.Host}/storage/avatars/{user.ProfilePictureFileName}"
            })
            .ToListAsync();

        return Ok(users);
    }
}