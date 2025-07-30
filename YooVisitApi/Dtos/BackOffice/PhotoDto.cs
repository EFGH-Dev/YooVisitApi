namespace YooVisitApi.Dtos.BackOffice
{
    public class PhotoDto
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public string? Url { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
