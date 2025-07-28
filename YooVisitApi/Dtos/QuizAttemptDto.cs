using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos
{
    public class QuizAttemptDto
    {
        [Required]
        public Guid SelectedAnswerId { get; set; }
    }
}
