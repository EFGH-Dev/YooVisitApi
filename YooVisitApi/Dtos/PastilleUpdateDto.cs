using System.ComponentModel.DataAnnotations;

namespace YooVisitAPI.Dtos
{
    public class PastilleUpdateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
