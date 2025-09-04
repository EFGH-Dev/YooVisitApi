using YooVisitApi.Dtos.Shared;

namespace YooVisitApi.Dtos.Zone;

public class ZoneResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserId { get; set; }
    public List<LatLngDto> Coordinates { get; set; } // On renvoie une vraie liste, pas du JSON !
}