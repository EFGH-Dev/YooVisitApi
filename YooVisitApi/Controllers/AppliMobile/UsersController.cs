using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos.User;
using YooVisitApi.Interfaces;
using YooVisitApi.Models.UserModel;

namespace YooVisitApi.Controllers.AppliMobile;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public UsersController(ApiDbContext context, ITokenService tokenService, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _tokenService = tokenService;
        _hostingEnvironment = hostingEnvironment;
    }

    // ...

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        if (existingUser != null)
        {
            return BadRequest("Cet e-mail est déjà utilisé.");
        }

        var user = new UserApplication
        {
            IdUtilisateur = Guid.NewGuid(),
            Email = registerDto.Email,
            Nom = registerDto.Nom,
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password), // Correction ici
            DateInscription = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201, "Utilisateur créé avec succès.");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

        if (user == null) return NotFound();

        return Ok(new UserDto
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
        });
    }

    [Authorize]
    [HttpGet("my-stats")]
    public async Task<ActionResult<PlayerStatsDto>> GetMyStats()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

        if (user == null) return NotFound("Utilisateur non trouvé.");

        var pastillesCount = await _context.Pastilles.CountAsync(p => p.CreatedByUserId == userId);

        var stats = new PlayerStatsDto
        {
            UserName = user.Nom ?? user.Email.Split('@').First(),
            Experience = user.Experience,
            AccessPoints = pastillesCount,
            ExplorationProgress = pastillesCount > 50 ? 1.0 : pastillesCount / 50.0, // Logique de progression simple
            ProfilePictureUrl = user.ProfilePictureFileName == null
                ? null
                : $"{Request.Scheme}://{Request.Host}/storage/avatars/{user.ProfilePictureFileName}"
        };

        return Ok(stats);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto updateDto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return NotFound();

        if (updateDto.Nom != null) user.Nom = updateDto.Nom;
        if (updateDto.Biographie != null) user.Biographie = updateDto.Biographie;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Aucun fichier fourni.");
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound("Utilisateur non trouvé.");
        }

        // On crée un nom de fichier unique pour éviter les conflits
        var fileExtension = Path.GetExtension(file.FileName);
        var newFileName = $"{userId}{fileExtension}";

        // On s'assure que le dossier de stockage existe
        // IMPORTANT : Ce chemin pointe vers un dossier DANS le conteneur.
        // On doit monter un volume Docker sur ce dossier pour que les fichiers persistent.
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "storage", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, newFileName);

        // On sauvegarde le fichier sur le disque
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // On met à jour la fiche du joueur avec le nom du fichier
        user.ProfilePictureFileName = newFileName;
        await _context.SaveChangesAsync();

        // On renvoie l'URL publique de la nouvelle image
        var newUrl = $"{Request.Scheme}://{Request.Host}/storage/avatars/{newFileName}";
        return Ok(new { profilePictureUrl = newUrl });
    }

}
