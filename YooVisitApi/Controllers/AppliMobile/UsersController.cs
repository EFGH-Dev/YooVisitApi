using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos.User;
using YooVisitApi.Interfaces;
using YooVisitApi.Models.UserModel;
using YooVisitApi.Services;


namespace YooVisitApi.Controllers.AppliMobile;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApiDbContext context, ITokenService tokenService, IWebHostEnvironment hostingEnvironment, IObjectStorageService storageService, ILogger<UsersController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _hostingEnvironment = hostingEnvironment;
        _storageService = storageService;
        _logger = logger;
    }

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
                : _storageService.GeneratePresignedGetUrl(user.ProfilePictureFileName)
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
                : _storageService.GeneratePresignedGetUrl(user.ProfilePictureFileName)
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
        _logger.LogInformation(">>> Début de l'upload de la photo de profil.");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Aucun fichier fourni dans la requête.");
            return BadRequest("Aucun fichier fourni.");
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            _logger.LogWarning($"Utilisateur non trouvé pour l'ID : {userId}");
            return NotFound("Utilisateur non trouvé.");
        }

        _logger.LogInformation($"Utilisateur trouvé : {user.Email}.");

        // Si l'utilisateur avait déjà une photo, on supprime l'ancienne de S3
        if (!string.IsNullOrEmpty(user.ProfilePictureFileName))
        {
            _logger.LogInformation($"Suppression de l'ancien avatar : {user.ProfilePictureFileName}");
            await _storageService.DeleteFileAsync(user.ProfilePictureFileName);
        }

        var fileExtension = Path.GetExtension(file.FileName);
        var fileKey = $"avatars/{Guid.NewGuid()}{fileExtension}";
        _logger.LogInformation($"Nouvelle clé de fichier générée : {fileKey}");

        try
        {
            _logger.LogInformation("--- Tentative d'upload vers S3... ---");
            await _storageService.UploadFileAsync(file.OpenReadStream(), fileKey, file.ContentType);
            _logger.LogInformation("--- UPLOAD RÉUSSI ---");
        }
        catch (Exception ex)
        {
            // CETTE PARTIE EST LA PLUS IMPORTANTE !
            // On log l'erreur complète si l'upload S3 échoue
            _logger.LogError(ex, "!!!! ERREUR LORS DE L'UPLOAD SUR S3 !!!!");
            return StatusCode(500, "Une erreur est survenue lors de la sauvegarde de l'image.");
        }

        _logger.LogInformation("Mise à jour de l'utilisateur en base de données...");
        user.ProfilePictureFileName = fileKey;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Mise à jour de la base de données réussie.");

        var newUrl = _storageService.GeneratePresignedGetUrl(fileKey);
        return Ok(new { profilePictureUrl = newUrl });
    }

}
