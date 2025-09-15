using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitApi.Data;
using YooVisitApi.Dtos.Quiz;
using YooVisitApi.Models;
using YooVisitApi.Models.PastilleModel;
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
            Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            Pastille? pastille = await _context.Pastilles.FindAsync(pastilleId);
            if (pastille == null) return NotFound("Pastille non trouvée.");
            if (pastille.CreatedByUserId != userId) return Forbid("Seul le créateur de la pastille peut y ajouter un quiz.");

            // Conversion sécurisée du type de quiz
            if (!Enum.TryParse<QuizType>(dto.QuizType, true, out QuizType quizType))
            {
                return BadRequest("La valeur de 'quizType' est invalide.");
            }

            Quiz quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                PastilleId = pastilleId,
                Title = dto.Title,
                Description = dto.Description,
                QuestionText = dto.QuestionText,
                Type = quizType // On assigne l'enum
            };

            // Logique de création des réponses en fonction du type de quiz
            switch (quizType)
            {
                case QuizType.QCM:
                    // Pour un QCM, on s'attend à plusieurs réponses
                    if (dto.Answers.Count < 2) return BadRequest("Un QCM doit avoir au moins 2 réponses.");
                    for (int i = 0; i < dto.Answers.Count; i++)
                    {
                        quiz.Answers.Add(new QuizAnswer
                        {
                            Id = Guid.NewGuid(),
                            AnswerText = dto.Answers[i],
                            IsCorrect = i == dto.CorrectAnswerIndex
                        });
                    }
                    break;

                case QuizType.VraiFaux:
                    // Pour un Vrai/Faux, les réponses sont fixes
                    quiz.Answers.Add(new QuizAnswer { Id = Guid.NewGuid(), AnswerText = "Vrai", IsCorrect = (dto.CorrectAnswerIndex == 0) });
                    quiz.Answers.Add(new QuizAnswer { Id = Guid.NewGuid(), AnswerText = "Faux", IsCorrect = (dto.CorrectAnswerIndex == 1) });
                    break;

                case QuizType.TexteLibre:
                    // Pour un texte libre, il n'y a qu'une seule réponse, et c'est la bonne
                    if (dto.Answers.Count != 1) return BadRequest("Un quiz de type Texte Libre ne doit avoir qu'une seule réponse.");
                    quiz.Answers.Add(new QuizAnswer
                    {
                        Id = Guid.NewGuid(),
                        AnswerText = dto.Answers[0],
                        IsCorrect = true // L'unique réponse est la bonne
                    });
                    break;
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            // On renvoie un DTO propre et à jour
            QuizDto quizDto = MapToDto(quiz);
            return CreatedAtAction(nameof(GetQuizForPastille), new { pastilleId = pastille.Id, quizId = quiz.Id }, quizDto);
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
                QuizType = quiz.Type.ToString(), // <-- On ajoute le type de quiz ici !
                Answers = quiz.Answers?.Select(a => new QuizAnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText
                }).ToList() ?? new List<QuizAnswerDto>()
            };
        }
    }
}
