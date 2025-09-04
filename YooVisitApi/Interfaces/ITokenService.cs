using YooVisitApi.Models.UserModel;

namespace YooVisitApi.Interfaces;

public interface ITokenService
{
    // Contrat pour générer un token.
    // Prend un utilisateur en entrée pour mettre ses infos (ID, rôle...) dans le token.
    // Renvoie le token (string) et sa date d'expiration.
    (string token, DateTime expiration) GenerateJwtToken(UserApplication user);
    string GenerateRefreshToken();
}