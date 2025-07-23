namespace YooVisitApi.Dtos
{
    public class PhotoDto
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; }
        public bool IsOwner { get; set; }
        public string? UserName { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
