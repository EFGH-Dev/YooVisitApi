using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Quiz
{
    public class QuizAttemptDto
    {
        [Required]
        public Guid SelectedAnswerId { get; set; }
    }
}
