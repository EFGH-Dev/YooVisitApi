// Fichier: Controllers/BackOfficePastillesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Models.PastilleModel;
using YooVisitApi.Dtos.BackOffice;
using YooVisitApi.Filters;

[ApiController]
[Route("api/backoffice/pastilles")]
[ApiKeyAuthorize]
public class BackOfficePastillesController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public BackOfficePastillesController(ApiDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    // GET: api/backoffice/pastilles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PastilleListDto>>> GetPastilles()
    {
        return await _context.Pastilles
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PastilleListDto
            {
                Id = p.Id,
                Title = p.Title,
                NomCreateur = p.User.Nom,
                NombreDeQuiz = p.Quizzes.Count(),
                NoteMoyenne = p.Ratings.Any() ? p.Ratings.Average(r => r.RatingValue) : 0
            })
            .ToListAsync();
    }

    // GET: api/backoffice/pastilles/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PastilleDetailDto>> GetPastilleById(Guid id)
    {
        var pastilleDto = await _context.Pastilles
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PastilleDetailDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                ExternalLink = p.ExternalLink,
                Photos = p.Photos.Select(ph => new PhotoDto
                {
                    Id = ph.Id,
                    FileName = ph.FileName,
                    Url = $"{Request.Scheme}://{Request.Host}/storage/{ph.FileName}",
                    UploadedAt = ph.UploadedAt
                }).ToList(),
                Quizzes = p.Quizzes.Select(q => new QuizListDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    ReponsesPossibles = q.Answers.Count
                }).ToList()
            }).FirstOrDefaultAsync();

        if (pastilleDto == null)
        {
            return NotFound();
        }

        return Ok(pastilleDto);
    }

    // PUT: api/backoffice/pastilles/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePastille(Guid id, [FromBody] PastilleDetailDto pastilleDto)
    {
        if (id != pastilleDto.Id)
        {
            return BadRequest("L'ID de l'URL ne correspond pas à l'ID du corps de la requête.");
        }

        var pastilleInDb = await _context.Pastilles.FindAsync(id);

        if (pastilleInDb == null)
        {
            return NotFound();
        }

        // Mapping des propriétés
        pastilleInDb.Title = pastilleDto.Title;
        pastilleInDb.Description = pastilleDto.Description;
        pastilleInDb.Latitude = pastilleDto.Latitude;
        pastilleInDb.Longitude = pastilleDto.Longitude;
        pastilleInDb.ExternalLink = pastilleDto.ExternalLink;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Pastilles.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/backoffice/pastilles/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePastille(Guid id)
    {
        var pastille = await _context.Pastilles.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == id);
        if (pastille == null)
        {
            return NotFound();
        }

        // Supprimer les fichiers physiques associés
        foreach (var photo in pastille.Photos)
        {
            var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "storage", photo.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // La BDD est configurée pour supprimer en cascade les ratings, photos, etc.
        _context.Pastilles.Remove(pastille);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}