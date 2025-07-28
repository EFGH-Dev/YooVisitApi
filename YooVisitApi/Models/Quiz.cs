using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Models
{
    public class Quiz
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string QuestionText { get; set; }

        [Required]
        public Guid PastilleId { get; set; }
        public virtual Pastille Pastille { get; set; }

        public virtual ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }
}
