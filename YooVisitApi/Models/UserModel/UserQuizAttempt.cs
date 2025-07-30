using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Models.UserModel
{
    public class UserQuizAttempt
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid QuizId { get; set; }
        [Required]
        public Guid SelectedAnswerId { get; set; }
        public DateTime AttemptedAt { get; set; }
        public bool WasCorrect { get; set; }
    }
}
