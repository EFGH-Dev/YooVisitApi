using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Login;
using YooVisitApi.Dtos.User;
using YooVisitApi.Interfaces;

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
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null)
        {
            return Unauthorized("Email ou mot de passe invalide.");
        }
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.HashedPassword))
        {
            return Unauthorized("Email ou mot de passe invalide.");
        }

        (string token, DateTime expiration) = _tokenService.GenerateJwtToken(user);

        var userDto = new UserDto
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

        return Ok(new LoginResponseDto { Token = token, Expiration = expiration, User = userDto });
    }
}
