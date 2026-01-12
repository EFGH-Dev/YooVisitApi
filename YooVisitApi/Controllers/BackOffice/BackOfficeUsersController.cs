using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Models.UserModel;
using YooVisitApi.Dtos.BackOffice;
using YooVisitApi.Filters;

[ApiController]
[Route("api/backoffice/users")]
[ApiKeyAuthorize]
public class BackOfficeUsersController : ControllerBase
{
    private readonly ApiDbContext _context;

    public BackOfficeUsersController(ApiDbContext context)
    {
        _context = context;
    }

    // GET: api/backoffice/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Nom)
            .Select(u => new UserListDto
            {
                IdUtilisateur = u.IdUtilisateur,
                Nom = u.Nom,
                Email = u.Email,
                DateInscription = u.DateInscription,
                Experience = u.Experience
            })
            .ToListAsync();
    }

    // GET: api/backoffice/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> GetUserById(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.IdUtilisateur == id)
            .Select(u => new UserDetailDto
            {
                IdUtilisateur = u.IdUtilisateur,
                Nom = u.Nom,
                Email = u.Email,
                DateInscription = u.DateInscription,
                Experience = u.Experience,
                Role = "Joueur", // Logique de rôle à implémenter si nécessaire
                Statut = "Actif"  // Logique de statut à implémenter si nécessaire
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // DELETE: api/backoffice/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // ATTENTION : Si un utilisateur a créé des pastilles, la suppression directe
        // peut échouer à cause des contraintes de clé étrangère.
        // Une stratégie de "soft delete" (marquer comme inactif) est plus sûre.
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log l'erreur et retourne un message clair
            return Conflict("Impossible de supprimer cet utilisateur car il est lié à d'autres données (ex: des pastilles). " + ex.Message);
        }

        return NoContent();
    }
}