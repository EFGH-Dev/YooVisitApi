using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Models.ZoneModel;
using YooVisitApi.Dtos.BackOffice;
using YooVisitApi.Filters;

[ApiController]
[Route("api/backoffice/zones")]
[ApiKeyAuthorize]
public class BackOfficeZonesController : ControllerBase
{
    private readonly ApiDbContext _context;

    public BackOfficeZonesController(ApiDbContext context)
    {
        _context = context;
    }

    // GET: api/backoffice/zones
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZoneDto>>> GetZones()
    {
        // Jointure pour récupérer le nom de l'utilisateur créateur
        return await (from zone in _context.Zones
                      join user in _context.Users on zone.CreatedByUserId equals user.IdUtilisateur
                      orderby zone.CreatedAt descending
                      select new ZoneDto
                      {
                          Id = zone.Id,
                          Name = zone.Name,
                          CoordinatesJson = zone.CoordinatesJson,
                          CreatedAt = zone.CreatedAt,
                          CreatedByUserName = user.Nom
                      })
                      .AsNoTracking()
                      .ToListAsync();
    }

    // GET: api/backoffice/zones/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ZoneDto>> GetZoneById(Guid id)
    {
        var zone = await (from z in _context.Zones
                          join user in _context.Users on z.CreatedByUserId equals user.IdUtilisateur
                          where z.Id == id
                          select new ZoneDto
                          {
                              Id = z.Id,
                              Name = z.Name,
                              CoordinatesJson = z.CoordinatesJson,
                              CreatedAt = z.CreatedAt,
                              CreatedByUserName = user.Nom
                          })
                          .AsNoTracking()
                          .FirstOrDefaultAsync();

        if (zone == null)
        {
            return NotFound();
        }

        return Ok(zone);
    }

    // POST: api/backoffice/zones
    [HttpPost]
    public async Task<IActionResult> CreateZone([FromBody] ZoneDto zoneDto)
    {
        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = zoneDto.Name,
            CoordinatesJson = zoneDto.CoordinatesJson,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.Parse("00000000-0000-0000-0000-000000000001") // ID d'un utilisateur admin, à gérer
        };

        _context.Zones.Add(zone);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetZoneById), new { id = zone.Id }, zoneDto);
    }

    // PUT: api/backoffice/zones/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateZone(Guid id, [FromBody] ZoneDto zoneDto)
    {
        if (id != zoneDto.Id)
        {
            return BadRequest();
        }

        var zoneInDb = await _context.Zones.FindAsync(id);
        if (zoneInDb == null)
        {
            return NotFound();
        }

        zoneInDb.Name = zoneDto.Name;
        zoneInDb.CoordinatesJson = zoneDto.CoordinatesJson;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/backoffice/zones/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteZone(Guid id)
    {
        var zone = await _context.Zones.FindAsync(id);
        if (zone == null)
        {
            return NotFound();
        }

        _context.Zones.Remove(zone);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}