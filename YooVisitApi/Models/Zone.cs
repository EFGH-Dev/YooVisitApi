using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Models
{
    public class Zone
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // On stocke la liste des points comme une chaîne de texte JSON.
        // C'est la méthode la plus simple et la plus flexible.
        [Required]
        public string CoordinatesJson { get; set; } = string.Empty;

        [Required]
        public Guid CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
