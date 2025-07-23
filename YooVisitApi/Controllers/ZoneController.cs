using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using YooVisitApi.Data;
using YooVisitApi.Dtos;
using YooVisitApi.Models;

namespace YooVisitApi.Controllers;

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
    public async Task<IActionResult> GetAllZones()
    {
        var zones = await _context.Zones.ToListAsync();
        // Note : On renvoie l'objet Zone complet pour l'instant.
        // On pourrait créer un ZoneDto si on voulait cacher certaines informations.
        return Ok(zones);
    }

    // --- Endpoint pour récupérer une zone par son ID (utile pour le CreatedAtAction) ---
    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(Guid id)
    {
        var zone = await _context.Zones.FindAsync(id);
        if (zone == null)
        {
            return NotFound();
        }
        return Ok(zone);
    }
}