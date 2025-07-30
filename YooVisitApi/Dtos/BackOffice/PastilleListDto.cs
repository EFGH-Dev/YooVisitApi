namespace YooVisitApi.Dtos.BackOffice
{
    public class PastilleListDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? NomCreateur { get; set; }
        public int NombreDeQuiz { get; set; }
        public double NoteMoyenne { get; set; }
    }
}
