using System.ComponentModel.DataAnnotations;

namespace YooVisitAPI.Dtos
{
    public class UpdateProfileDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Biographie { get; set; }
    }
}
