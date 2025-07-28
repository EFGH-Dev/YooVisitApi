using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Models
{
    public class Pastille
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? StyleArchitectural { get; set; }
        public string? PeriodeConstruction { get; set; }
        public string? HorairesOuverture { get; set; }
        [Required]
        public Guid CreatedByUserId { get; set; }
        public virtual UserApplication? User { get; set; }
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
        public virtual ICollection<PastilleRating> Ratings { get; set; } = new List<PastilleRating>();
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
