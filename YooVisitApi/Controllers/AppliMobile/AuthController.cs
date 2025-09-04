using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Login;
using YooVisitApi.Dtos.User;
using YooVisitApi.Interfaces;
using YooVisitApi.Models.UserModel;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApiDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(IConfiguration configuration, ApiDbContext context, ITokenService tokenService)
    {
        _configuration = configuration;
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginDto)
    {
        UserApplication? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.HashedPassword))
        {
            return Unauthorized("Email ou mot de passe invalide.");
        }

        (string accessToken, DateTime accessTokenExpiration) = _tokenService.GenerateJwtToken(user);
        string refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        UserDto userDto = new UserDto
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
        };

        return Ok(new LoginResponseDto
        {
            Token = accessToken,
            Expiration = accessTokenExpiration,
            RefreshToken = refreshToken,
            User = userDto
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult> Refresh([FromBody] RefreshTokenRequestDto tokenDto)
    {
        // 1. Chercher l'utilisateur via son refresh token
        UserApplication? user = await _context.Users.SingleOrDefaultAsync(u => u.RefreshToken == tokenDto.RefreshToken);

        // 2. Valider le token
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized("Session expirée. Veuillez vous reconnecter.");
        }

        // 3. Générer une NOUVELLE paire de jetons
        (string newAccessToken, DateTime newAccessTokenExpiration) = _tokenService.GenerateJwtToken(user);
        string newRefreshToken = _tokenService.GenerateRefreshToken();

        // 4. Mettre à jour le refresh token en BDD (rotation pour la sécurité)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        // 5. Renvoyer la nouvelle paire de jetons
        return Ok(new
        {
            Token = newAccessToken,
            Expiration = newAccessTokenExpiration,
            RefreshToken = newRefreshToken
        });
    }
}
