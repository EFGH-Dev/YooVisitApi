using System;
using System.ComponentModel.DataAnnotations;

namespace YooVisitAPI.Models;

public class Photo
{
    [Key]
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }
    [Required]
    public Guid PastilleId { get; set; }
    public virtual Pastille Pastille { get; set; }
}