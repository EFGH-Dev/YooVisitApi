namespace YooVisitApi.Dtos.BackOffice
{
    public class PastilleCreateDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        // On pourrait même demander l'ID de l'utilisateur créateur
        public Guid CreatedByUserId { get; set; }
    }
}
