using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Dtos.BackOffice;
using YooVisitApi.Filters;

[ApiController]
[Route("api/backoffice")]
[ApiKeyAuthorize]
public class BackOfficeDashboardController : ControllerBase
{
    private readonly ApiDbContext _context;

    public BackOfficeDashboardController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        var stats = new DashboardStatsDto
        {
            TotalPastilles = await _context.Pastilles.CountAsync(),
            TotalUtilisateurs = await _context.Users.CountAsync(),
            TotalQuiz = await _context.Quizzes.CountAsync(),
            NoteMoyennePastilles = await _context.PastilleRatings.AnyAsync()
                ? Math.Round(await _context.PastilleRatings.AverageAsync(r => r.RatingValue), 2)
                : 0
        };
        return Ok(stats);
    }

    [HttpGet("recent-activities")]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivities()
    {
        // Utilise Pastille.CreatedAt (que vous devez ajouter)
        var recentPastilles = await _context.Pastilles
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivityDto
            {
                TypeActivite = "Nouvelle Pastille",
                Description = p.Title,
                NomUtilisateur = p.User.Nom, // On utilise p.User.Nom qui existe
                Date = p.CreatedAt
            }).ToListAsync();

        // Utilise UserApplication.DateInscription
        var recentUsers = await _context.Users
            .OrderByDescending(u => u.DateInscription)
            .Take(5)
            .Select(u => new RecentActivityDto
            {
                TypeActivite = "Nouvel Utilisateur",
                Description = u.Nom,
                NomUtilisateur = u.Nom,
                Date = u.DateInscription
            }).ToListAsync();

        var activities = recentPastilles.Concat(recentUsers)
            .OrderByDescending(a => a.Date)
            .Take(10);

        return Ok(activities);
    }
}