using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Zone;

// Représente un point GPS simple
public class LatLngDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ZoneCreateRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(3)] // Un polygone doit avoir au moins 3 points
    public List<LatLngDto> Coordinates { get; set; } = new();
}