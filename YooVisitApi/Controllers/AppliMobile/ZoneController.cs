using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Shared;
using YooVisitApi.Dtos.Zone;
using YooVisitApi.Hubs;
using YooVisitApi.Models.ZoneModel;

namespace YooVisitApi.Controllers.AppliMobile;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Toutes les actions de ce contrôleur nécessitent d'être connecté
public class ZonesController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IHubContext<Updates> _hubContext;

    public ZonesController(ApiDbContext context, IHubContext<Updates> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
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
        Console.WriteLine($"---> [SignalR] Attempting to send Zone Created notification for ID: {zone.Id}");

        // --- 🚀 SIGNALR : Création ---
        // 1. Projection DTO (incluant les coordonnées)
        ZoneResponseDto responseDto = new ZoneResponseDto
        {
            Id = zone.Id,
            Name = zone.Name,
            CreatedAt = zone.CreatedAt,
            CreatedByUserId = zone.CreatedByUserId.ToString(),
            Coordinates = JsonSerializer.Deserialize<List<LatLngDto>>(zone.CoordinatesJson) ?? new List<LatLngDto>()
        };

        // 2. Émission du message
        await _hubContext.Clients.All.SendAsync(
            "UpdateReceived", // Nom de la méthode écoutée par le client
            new UpdateReceivedDto
            {
                EntityType = "Zone",
                Action = "Created",
                Payload = responseDto
            }
        );
        // -----------------------------
        Console.WriteLine($"---> [SignalR] Sent Zone Created notification.");

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

    // --- Endpoint pour MODIFIER une zone existante ---
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateZone(Guid id, [FromBody] ZoneCreateRequestDto request)
    {
        // 1. On vérifie si la zone existe
        Zone? zone = await _context.Zones.FindAsync(id);
        if (zone == null)
        {
            return NotFound();
        }

        // 2. Vérification des droits (Seul le créateur peut modifier sa zone, ou un Admin)
        Guid currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // TODO: Implémenter la vérification du rôle Admin si nécessaire
        if (zone.CreatedByUserId != currentUserId)
        {
            // Typage fort : on retourne une 403 Forbidden si l'utilisateur n'est pas le créateur
            return StatusCode(403, new { message = "Seul le créateur de la zone est autorisé à la modifier." });
        }

        // 3. Mise à jour des propriétés
        zone.Name = request.Name;
        // Re-sérialisation des nouvelles coordonnées
        zone.CoordinatesJson = JsonSerializer.Serialize(request.Coordinates);

        // On marque l'entité comme modifiée
        _context.Entry(zone).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Si la zone n'existe plus au moment de la sauvegarde
            if (!_context.Zones.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw; // L'exception est plus grave, on la propage
            }
        }

        // --- 🚀 SIGNALR : Mise à jour ---
        // 1. Projection DTO (on utilise l'entité 'zone' mise à jour)
        ZoneResponseDto responseDto = new ZoneResponseDto
        {
            Id = zone.Id,
            Name = zone.Name,
            CreatedAt = zone.CreatedAt,
            CreatedByUserId = zone.CreatedByUserId.ToString(),
            Coordinates = JsonSerializer.Deserialize<List<LatLngDto>>(zone.CoordinatesJson) ?? new List<LatLngDto>()
        };

        // 2. Émission du message
        await _hubContext.Clients.All.SendAsync(
            "UpdateReceived",
            new UpdateReceivedDto
            {
                EntityType = "Zone",
                Action = "Updated", // Action 'Updated'
                Payload = responseDto
            }
        );
        // -----------------------------

        // Typage fort : 204 No Content pour une mise à jour réussie
        return NoContent();
    }

    // --- Endpoint pour SUPPRIMER une zone ---
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteZone(Guid id)
    {
        Zone? zone = await _context.Zones.FindAsync(id);
        if (zone == null)
        {
            // Si elle n'existe pas, on ne peut pas la supprimer, mais on renvoie 204 pour idempotence (souvent un 404 est acceptable aussi)
            return NotFound();
        }

        // 1. Vérification des droits (Seul le créateur peut supprimer sa zone, ou un Admin)
        Guid currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // TODO: Implémenter la vérification du rôle Admin si nécessaire
        if (zone.CreatedByUserId != currentUserId)
        {
            // 403 Forbidden
            return StatusCode(403, new { message = "Seul le créateur de la zone est autorisé à la supprimer." });
        }

        string zoneIdString = zone.Id.ToString();

        // 2. Suppression et sauvegarde
        _context.Zones.Remove(zone);
        await _context.SaveChangesAsync();

        // --- 🚀 SIGNALR : Suppression ---
        // 1. On envoie juste l'ID pour la suppression côté client
        await _hubContext.Clients.All.SendAsync(
            "UpdateReceived",
            new UpdateReceivedDto
            {
                EntityType = "Zone",
                Action = "Deleted", // Action 'Deleted'
                Payload = new IdDto { Id = zoneIdString } // Utiliser un DTO simple avec l'ID (ou l'envoyer directement)
            }
        );
        // -----------------------------

        // 204 No Content est la réponse standard pour une suppression réussie
        return NoContent();
    }
}