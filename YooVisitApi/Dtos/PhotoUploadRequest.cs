using Microsoft.AspNetCore.Http;

namespace YooVisitAPI.Dtos;

public class PhotoUploadRequest
{
    // Le fichier envoyé par Flutter
    public IFormFile File { get; set; }
    // La latitude envoyée comme champ de formulaire
    public double Latitude { get; set; }
    // La longitude envoyée comme champ de formulaire
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public string Title { get; set; } = string.Empty;
}