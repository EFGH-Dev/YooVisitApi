namespace YooVisitApi.Dtos
{
    public class QuizAttemptResultDto
    {
        public bool WasCorrect { get; set; }
        public int ExperienceGained { get; set; }
        public Guid CorrectAnswerId { get; set; } // On révèle la bonne réponse après la tentative.
    }
}
