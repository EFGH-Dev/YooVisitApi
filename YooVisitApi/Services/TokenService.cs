// --- Nouveaux Usings pour le pattern IOptions ---
using Microsoft.Extensions.Options; // Pour IOptions<T>
// ---------------------------------------------
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using YooVisitApi.Configuration;  // Pour ta classe JwtSettings
using YooVisitApi.Interfaces; // Pour l'interface
using YooVisitApi.Models.UserModel;   // Pour UserApplication

namespace YooVisitApi.Services
{
    public class TokenService : ITokenService
    {
        // 1. DÉPENDANCE FORTEMENT TYPÉE
        // Fini IConfiguration, on utilise notre classe POCO
        private readonly JwtSettings _jwtSettings;

        // 2. LE CONSTRUCTEUR REFACTORISÉ
        // On demande IOptions<JwtSettings> au lieu de IConfiguration
        public TokenService(IOptions<JwtSettings> jwtOptions)
        {
            // On "déballe" l'objet pour l'utiliser
            this._jwtSettings = jwtOptions.Value;
        }

        public (string token, DateTime expiration) GenerateJwtToken(UserApplication user)
        {
            // 3. UTILISATION FORTEMENT TYPÉE
            // Fini les magic strings ! C'est du C# pur.
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._jwtSettings.Key));
            SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            DateTime expirationTime = DateTime.UtcNow.AddHours(24);

            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUtilisateur.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Nom ?? user.Email.Split('@').First())
            };

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                // 4. UTILISATION FORTEMENT TYPÉE
                Issuer = this._jwtSettings.Issuer,
                Audience = this._jwtSettings.Audience,
                SigningCredentials = credentials
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            return (tokenHandler.WriteToken(token), expirationTime);
        }

        // Cette méthode est parfaite, elle n'a pas de dépendances externes.
        public string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[64];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}