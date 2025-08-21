using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Storage
{
    public class UploadUrlRequestDto
    {
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string ContentType { get; set; } = string.Empty;
    }
}