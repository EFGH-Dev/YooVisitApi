namespace YooVisitApi.Dtos.BackOffice
{
    public class QuizAnswerDto
    {
        public Guid Id { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        // On expose le statut de la réponse car c'est un back-office
        public bool IsCorrect { get; set; }
    }
}
