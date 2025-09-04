using System.ComponentModel.DataAnnotations;
using YooVisitApi.Models.PastilleModel;

namespace YooVisitApi.Models.UserModel;

public class UserApplication
{
    [Key]
    public Guid IdUtilisateur { get; set; }

    [Required]
    public string Email { get; set; }

    public string HashedPassword { get; set; }

    public DateTime DateInscription { get; set; }
    public int Experience { get; set; } = 0;

    [Required]
    [StringLength(50)]
    public string Nom { get; set; }

    [StringLength(500)]
    public string? Biographie { get; set; }

    [StringLength(200)]
    public string? ProfilePictureFileName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public virtual ICollection<Pastille> Pastilles { get; set; } = new List<Pastille>();
}
