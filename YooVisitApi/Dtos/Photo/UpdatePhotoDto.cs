using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Pastille
{
    public class UpdatePhotoDto
    {
        [Required]
        public string FileKey { get; set; }
    }
}