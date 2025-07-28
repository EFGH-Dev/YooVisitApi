using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Models
{
    public class QuizAnswer
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; } = false;

        [Required]
        public Guid QuizId { get; set; }
        public virtual Quiz Quiz { get; set; }
    }
}
