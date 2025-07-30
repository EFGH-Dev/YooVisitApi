using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YooVisitApi.Data;
using YooVisitApi.Models.QuizModel;
using YooVisitApi.Dtos.BackOffice;

[ApiController]
[Route("api/backoffice/quizzes")]
[ApiKeyAuthorize]
public class BackOfficeQuizzesController : ControllerBase
{
    private readonly ApiDbContext _context;

    public BackOfficeQuizzesController(ApiDbContext context)
    {
        _context = context;
    }

    // GET: api/backoffice/quizzes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuizListDto>>> GetQuizzes()
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Select(q => new QuizListDto
            {
                Id = q.Id,
                Title = q.Title,
                ReponsesPossibles = q.Answers.Count()
            })
            .ToListAsync();
    }

    // GET: api/backoffice/quizzes/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<QuizDetailDto>> GetQuizById(Guid id)
    {
        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Answers)
            .Where(q => q.Id == id)
            .Select(q => new QuizDetailDto
            {
                Id = q.Id,
                PastilleId = q.PastilleId,
                Title = q.Title,
                Description = q.Description,
                QuestionText = q.QuestionText,
                Answers = q.Answers.Select(a => new QuizAnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (quiz == null)
        {
            return NotFound();
        }

        return Ok(quiz);
    }

    // POST: api/backoffice/quizzes
    [HttpPost]
    public async Task<ActionResult<QuizDetailDto>> CreateQuiz([FromBody] QuizDetailDto quizDto)
    {
        // La création d'un quiz et de ses réponses doit être transactionnelle
        // pour s'assurer que tout est créé correctement, ou rien du tout.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                Title = quizDto.Title,
                Description = quizDto.Description,
                QuestionText = quizDto.QuestionText,
                PastilleId = quizDto.PastilleId
                // Ajoutez CreatedAt = DateTime.UtcNow si vous avez ajouté ce champ
            };

            foreach (var answerDto in quizDto.Answers)
            {
                quiz.Answers.Add(new QuizAnswer
                {
                    Id = Guid.NewGuid(),
                    AnswerText = answerDto.AnswerText,
                    IsCorrect = answerDto.IsCorrect
                });
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // On re-mappe vers le DTO pour le retour
            quizDto.Id = quiz.Id;
            foreach (var answer in quiz.Answers)
            {
                quizDto.Answers.First(a => a.AnswerText == answer.AnswerText).Id = answer.Id;
            }

            return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, quizDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Une erreur est survenue lors de la création du quiz. " + ex.Message);
        }
    }

    // PUT: api/backoffice/quizzes/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuiz(Guid id, [FromBody] QuizDetailDto quizDto)
    {
        if (id != quizDto.Id)
        {
            return BadRequest();
        }

        var quizInDb = await _context.Quizzes.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == id);
        if (quizInDb == null)
        {
            return NotFound();
        }

        // Mise à jour simple des propriétés du quiz
        quizInDb.Title = quizDto.Title;
        quizInDb.Description = quizDto.Description;
        quizInDb.QuestionText = quizDto.QuestionText;

        // NOTE : La mise à jour des réponses (Answers) est complexe.
        // Il faudrait comparer la liste existante avec la nouvelle,
        // supprimer les anciennes, ajouter les nouvelles, et mettre à jour les existantes.
        // Pour garder le code simple, cette logique n'est pas implémentée ici.
        // Envisagez des endpoints séparés pour gérer les réponses d'un quiz.

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/backoffice/quizzes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null)
        {
            return NotFound();
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}