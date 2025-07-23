using YooVisitApi.Dtos;
using YooVisitApi.Models;

namespace YooVisitApi.Services;

public interface IUserService
{
    // Contrat pour récupérer un utilisateur par son email.
    // Renvoie une Tâche (Task) qui contiendra un User ou null.
    Task<UserApplication> GetUserByEmailAsync(string email);

    // Contrat pour récupérer un utilisateur par son ID.
    Task<UserDto> GetUserByIdAsync(Guid id);

    // Contrat pour créer un nouvel utilisateur.
    // Prend le DTO d'inscription et le mot de passe haché en entrée.
    // Renvoie une Tâche qui contiendra le DTO de l'utilisateur créé.
    Task<UserDto> CreateUserAsync(RegisterUserDto userDto, string hashedPassword);
}