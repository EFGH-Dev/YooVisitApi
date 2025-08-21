using System;
using System.ComponentModel.DataAnnotations;
using YooVisitApi.Models.PastilleModel;

namespace YooVisitApi.Models.PhotoModel;

public class Photo
{
    [Key]
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string FileKey { get; set; }
    public DateTime UploadedAt { get; set; }
    [Required]
    public Guid PastilleId { get; set; }
    public virtual Pastille Pastille { get; set; }
}