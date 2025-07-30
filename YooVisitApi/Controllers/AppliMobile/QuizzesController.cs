using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Quiz;
using YooVisitApi.Models;
using YooVisitApi.Models.QuizModel;
using YooVisitApi.Models.UserModel;

namespace YooVisitApi.Controllers.AppliMobile
{
    [ApiController]
    [Route("api/pastilles/{pastilleId}/quizzes")] // Note la route "nichée"
    [Authorize]
    public class QuizzesController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public QuizzesController(ApiDbContext context)
        {
            _context = context;
        }

        // --- CRÉER un nouveau quiz pour une pastille ---
        [HttpPost]
        public async Task<IActionResult> CreateQuiz(Guid pastilleId, [FromBody] QuizCreateDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pastille = await _context.Pastilles.FindAsync(pastilleId);
            if (pastille == null) return NotFound("Pastille non trouvée.");
            if (pastille.CreatedByUserId != userId) return Forbid("Seul le créateur de la pastille peut y ajouter un quiz.");

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                PastilleId = pastilleId,
                Title = dto.Title,
                Description = dto.Description,
                QuestionText = dto.QuestionText,
            };

            for (int i = 0; i < dto.Answers.Count; i++)
            {
                quiz.Answers.Add(new QuizAnswer
                {
                    Id = Guid.NewGuid(),
                    AnswerText = dto.Answers[i],
                    IsCorrect = i == dto.CorrectAnswerIndex
                });
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            // On renvoie un DTO propre
            var quizDto = MapToDto(quiz);
            return CreatedAtAction(nameof(GetQuizzes), new { pastilleId = pastille.Id, quizId = quiz.Id }, quizDto);
        }

        // --- RÉCUPÉRER tous les quiz d'une pastille ---
        [HttpGet]
        public async Task<ActionResult<List<QuizDto>>> GetQuizzes(Guid pastilleId)
        {
            // 1. On récupère les données BRUTES de la BDD d'abord.
            // On appelle ToListAsync() AVANT le Select() qui utilise notre méthode custom.
            // Ceci "matérialise" la requête : les données quittent la BDD et arrivent dans une liste C#.
            var quizzesFromDb = await _context.Quizzes
                .Where(q => q.PastilleId == pastilleId)
                .Include(q => q.Answers)
                .AsNoTracking()
                .ToListAsync(); // <--- Le changement crucial est ici !

            // 2. PUIS, une fois les données en mémoire, on mappe les objets vers les DTOs.
            // Ce .Select() est maintenant un LINQ to Objects, pas un LINQ to Entities. Il s'exécute en C#.
            var quizzesDto = quizzesFromDb
                .Select(q => MapToDto(q))
                .ToList(); // Pas besoin de version async ici, c'est une opération en mémoire.

            return Ok(quizzesDto);
        }

        // --- RÉCUPÉRER un quiz spécifique ---
        [HttpGet("{quizId}")]
        public async Task<ActionResult<QuizDto>> GetQuizForPastille(Guid pastilleId, Guid quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId && q.PastilleId == pastilleId);

            if (quiz == null) return NotFound("Quiz non trouvé pour cette pastille.");

            return Ok(MapToDto(quiz));
        }

        // --- RÉPONDRE à un quiz ---
        [HttpPost("{quizId}/attempt")]
        public async Task<ActionResult<QuizAttemptResultDto>> AttemptQuiz(Guid pastilleId, Guid quizId, [FromBody] QuizAttemptDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var quiz = await _context.Quizzes
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.PastilleId == pastilleId);

            if (quiz == null) return NotFound("Quiz non trouvé.");

            var selectedAnswer = quiz.Answers.FirstOrDefault(a => a.Id == dto.SelectedAnswerId);
            if (selectedAnswer == null) return BadRequest("Réponse invalide.");

            var existingAttempt = await _context.UserQuizAttempts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.QuizId == quizId);
            if (existingAttempt != null) return Conflict("Vous avez déjà répondu à ce quiz.");

            bool wasCorrect = selectedAnswer.IsCorrect;
            int xpGained = 0;

            if (wasCorrect)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    xpGained = 25; // 25 XP pour une bonne réponse !
                    user.Experience += xpGained;
                }
            }

            _context.UserQuizAttempts.Add(new UserQuizAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizId = quizId,
                SelectedAnswerId = dto.SelectedAnswerId,
                AttemptedAt = DateTime.UtcNow,
                WasCorrect = wasCorrect
            });

            await _context.SaveChangesAsync();

            return Ok(new QuizAttemptResultDto
            {
                WasCorrect = wasCorrect,
                ExperienceGained = xpGained,
                CorrectAnswerId = quiz.Answers.First(a => a.IsCorrect).Id
            });
        }

        // --- Méthode d'aide pour mapper vers le DTO public ---
        private QuizDto MapToDto(Quiz quiz)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                PastilleId = quiz.PastilleId,
                Title = quiz.Title,
                Description = quiz.Description,
                QuestionText = quiz.QuestionText,
                Answers = quiz.Answers?.Select(a => new QuizAnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText
                }).ToList() ?? new List<QuizAnswerDto>()
            };
        }
    }
}
