namespace YooVisitApi.Dtos.Quiz
{
    public class QuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string QuestionText { get; set; }
        public Guid PastilleId { get; set; }
        public List<QuizAnswerDto> Answers { get; set; } = new();
    }
}
