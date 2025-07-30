namespace YooVisitApi.Dtos.BackOffice
{
    public class ZoneDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CoordinatesJson { get; set; } = "[]";
        public string CreatedByUserName { get; set; } = "N/A";
        public DateTime CreatedAt { get; set; }
    }
}
