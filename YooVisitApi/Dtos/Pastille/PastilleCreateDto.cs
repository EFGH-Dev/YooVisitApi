using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Pastille
{
    public class PastilleCreateDto
    {
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? StyleArchitectural { get; set; }
        public string? PeriodeConstruction { get; set; }
        public string? HorairesOuverture { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public string FileKey { get; set; } = string.Empty;
    }
}
