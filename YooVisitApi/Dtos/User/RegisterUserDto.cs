using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.User
{
    public class RegisterUserDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Nom { get; set; } = string.Empty;
        [Required]
        [EmailAddress] // ASP.NET Core vérifie que c'est un format d'email valide.
        public string Email { get; set; }

        [Required]
        [MinLength(8)] // On impose un mot de passe d'au moins 8 caractères.
        public string Password { get; set; }
    }
}
