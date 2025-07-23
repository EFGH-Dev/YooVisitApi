namespace YooVisitApi.Dtos
{
    public class PhotoDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
