using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Quiz
{
    public class QuizAttemptDto
    {
        public Guid? SelectedAnswerId { get; set; }
        public string? AnswerText { get; set; }
    }
}
