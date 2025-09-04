using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Shared;
using YooVisitApi.Dtos.Zone;
using YooVisitApi.Models.ZoneModel;

namespace YooVisitApi.Controllers.AppliMobile;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Toutes les actions de ce contrôleur nécessitent d'être connecté
public class ZonesController : ControllerBase
{
    private readonly ApiDbContext _context;

    public ZonesController(ApiDbContext context)
    {
        _context = context;
    }

    // --- Endpoint pour CRÉER une nouvelle zone ---
    [HttpPost]
    public async Task<IActionResult> CreateZone([FromBody] ZoneCreateRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            // On sérialise la liste des coordonnées en une chaîne JSON pour la stocker
            CoordinatesJson = JsonSerializer.Serialize(request.Coordinates),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Zones.Add(zone);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetZoneById), new { id = zone.Id }, zone);
    }

    // --- Endpoint pour RÉCUPÉRER toutes les zones ---
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZoneResponseDto>>> GetAllZones()
    {
        // ÉTAPE 1 : On exécute la requête SQL pour récupérer les données BRUTES.
        // Après cette ligne, on a une simple liste d'objets en mémoire.
        List<Zone> zonesFromDb = await _context.Zones.ToListAsync();

        // ÉTAPE 2 : On transforme cette liste en mémoire. 
        // Ici, on a le droit d'appeler n'importe quelle fonction C#.
        List<ZoneResponseDto> zonesDto = zonesFromDb.Select(zone => new ZoneResponseDto
        {
            Id = zone.Id,
            Name = zone.Name,
            CreatedAt = zone.CreatedAt,
            CreatedByUserId = zone.CreatedByUserId.ToString(),

            // Cette ligne fonctionne maintenant car elle n'est plus dans la requête SQL.
            Coordinates = JsonSerializer.Deserialize<List<LatLngDto>>(zone.CoordinatesJson)
                          ?? new List<LatLngDto>()

        }).ToList();

        return Ok(zonesDto);
    }

    // --- Endpoint pour RÉCUPÉRER une zone par son ID, mais en renvoyant un DTO ---
    [HttpGet("{id}")]
    public async Task<ActionResult<ZoneResponseDto>> GetZoneById(Guid id)
    {
        Zone? zone = await _context.Zones.FindAsync(id);
        if (zone == null)
        {
            return NotFound();
        }

        // On projette notre entité en DTO
        ZoneResponseDto responseDto = new ZoneResponseDto
        {
            Id = zone.Id,
            Name = zone.Name,
            CreatedAt = zone.CreatedAt,
            Coordinates = JsonSerializer.Deserialize<List<LatLngDto>>(zone.CoordinatesJson) ?? new List<LatLngDto>()
        };

        return Ok(responseDto);
    }

}