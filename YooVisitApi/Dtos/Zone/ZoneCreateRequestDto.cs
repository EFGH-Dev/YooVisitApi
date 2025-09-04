using System.ComponentModel.DataAnnotations;
using YooVisitApi.Dtos.Shared;

namespace YooVisitApi.Dtos.Zone;

public class ZoneCreateRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(3)] // Un polygone doit avoir au moins 3 points
    public List<LatLngDto> Coordinates { get; set; } = new();
}