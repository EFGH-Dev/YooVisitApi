using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos;

namespace YooVisitApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public PhotosController(ApiDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    [HttpGet("all-photos")]
    public async Task<IActionResult> GetAllPhotos()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // On doit maintenant joindre 3 tables : Photos -> Pastilles -> Users
        var photosWithDetails = await (from photo in _context.Photos
                                       join pastille in _context.Pastilles on photo.PastilleId equals pastille.Id
                                       join user in _context.Users on pastille.CreatedByUserId equals user.IdUtilisateur
                                       select new PhotoDto
                                       {
                                           Id = photo.Id,
                                           // On prend les données de la pastille
                                           Title = pastille.Title,
                                           Description = pastille.Description,
                                           Latitude = pastille.Latitude,
                                           Longitude = pastille.Longitude,
                                           // On prend les données de l'utilisateur
                                           UserName = user.Nom,
                                           // On prend les données de la photo elle-même
                                           ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{photo.FileName}",
                                           UploadedAt = photo.UploadedAt,
                                           IsOwner = pastille.CreatedByUserId == currentUserId
                                       }).ToListAsync();

        return Ok(photosWithDetails);
    }

    // --- ENDPOINT DE PHOTOS PERSONNELLES ---
    [Authorize]
    [HttpGet("my-photos")]
    public async Task<IActionResult> GetMyPhotos()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var userName = user.Nom ?? user.Email.Split('@').First();

        var myPhotos = await (from photo in _context.Photos
                              join pastille in _context.Pastilles on photo.PastilleId equals pastille.Id
                              where pastille.CreatedByUserId == userId
                              select new PhotoDto
                              {
                                  Id = photo.Id,
                                  Title = pastille.Title,
                                  Description = pastille.Description,
                                  Latitude = pastille.Latitude,
                                  Longitude = pastille.Longitude,
                                  UserName = userName,
                                  ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{photo.FileName}",
                                  UploadedAt = photo.UploadedAt,
                                  IsOwner = true
                              }).ToListAsync();

        return Ok(myPhotos);
    }
}
