using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Quiz
{
    public class QuizUpdateDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public string QuestionText { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        [Required]
        public string QuizType { get; set; } = string.Empty;
        [Required]
        [MinLength(1)]
        public List<string> Answers { get; set; } = new();
        [Required]
        public int CorrectAnswerIndex { get; set; }
    }
}
