using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Dtos;
using YooVisitApi.Models;// Assure-toi que le namespace de IUserService est bien importé

namespace YooVisitApi.Services;

// On déclare que cette classe implémente l'interface IUserService.
public class UserService : IUserService
{
    private readonly ApiDbContext _context; // On demande une instance du DbContext.

    // Le DbContext est "injecté" par le système de dépendances de .NET.
    public UserService(ApiDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> CreateUserAsync(RegisterUserDto userDto, string hashedPassword)
    {
        // 1. On crée une nouvelle instance de notre entité User.
        var user = new UserApplication
        {
            IdUtilisateur = Guid.NewGuid(),
            Email = userDto.Email,
            HashedPassword = hashedPassword,
            DateInscription = DateTime.UtcNow,
            Nom = userDto.Nom // On assigne le pseudo fourni
        };

        // 2. On ajoute ce nouvel utilisateur au "contexte" d'EF Core.
        _context.Users.Add(user);

        // 3. On sauvegarde les changements. C'est CETTE ligne qui exécute la requête SQL "INSERT".
        await _context.SaveChangesAsync();

        // 4. On "mappe" notre entité User vers un UserDto pour la renvoyer.
        // On ne renvoie jamais l'entité complète avec le mot de passe haché.
        return new UserDto
        {
            IdUtilisateur = user.IdUtilisateur,
            Email = user.Email,
            DateInscription = user.DateInscription,
            Nom = user.Nom
        };
    }

    public async Task<UserApplication?> GetUserByEmailAsync(string email)
    {
        // On retourne bien un UserApplication? car le controller a besoin du HashedPassword
        // pour la vérification, qui n'existe pas dans UserDto.
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            IdUtilisateur = user.IdUtilisateur,
            Email = user.Email,
            DateInscription = user.DateInscription,
            Nom = user.Nom,
        };
    }
}
