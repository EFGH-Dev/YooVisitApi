namespace YooVisitApi.Dtos.BackOffice
{
    public class PastilleDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? StyleArchitectural { get; set; }
        public string? PeriodeConstruction { get; set; }
        public string? HorairesOuverture { get; set; }

        // On utilise les autres DTOs pour les objets liés
        public List<PhotoDto> Photos { get; set; } = new();
        public List<QuizListDto> Quizzes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
