using YooVisitApi.Dtos.Quiz;

namespace YooVisitApi.Dtos.BackOffice
{
    public class QuizDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public Guid PastilleId { get; set; }

        public List<QuizAnswerDto> Answers { get; set; } = new();
    }
}
