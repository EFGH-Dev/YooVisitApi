using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Quiz
{
    public class QuizCreateDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public string QuestionText { get; set; } = string.Empty;

        // --- MODIFIÉ ---
        [Required]
        public string QuizType { get; set; } = string.Empty; // Le type envoyé par Flutter

        [Required]
        [MinLength(1, ErrorMessage = "Au moins une réponse est requise.")]
        public List<string> Answers { get; set; } = new List<string>(); // Taille variable

        [Required]
        [Range(0, int.MaxValue)]
        public int CorrectAnswerIndex { get; set; } // L'index de la bonne réponse
    }
}
