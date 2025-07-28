using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos
{
    public class QuizCreateDto
    {
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string QuestionText { get; set; }

        [Required]
        [MinLength(4)]
        [MaxLength(4)]
        public List<string> Answers { get; set; } // Les 4 réponses possibles

        [Required]
        [Range(0, 3)]
        public int CorrectAnswerIndex { get; set; } // L'index (de 0 à 3) de la bonne réponse
    }
}
