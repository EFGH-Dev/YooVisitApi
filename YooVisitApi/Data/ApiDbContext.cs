using Microsoft.EntityFrameworkCore;
using YooVisitApi.Models; // Assure-toi que ce namespace est correct

namespace YooVisitApi.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

    // On déclare toutes les tables de notre royaume
    public DbSet<UserApplication> Users { get; set; }
    public DbSet<Pastille> Pastilles { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<PastilleRating> PastilleRatings { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizAnswer> QuizAnswers { get; set; }
    public DbSet<UserQuizAttempt> UserQuizAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- On établit les traités diplomatiques entre les tables ---

        // Un utilisateur peut créer plusieurs pastilles
        modelBuilder.Entity<UserApplication>()
            .HasMany(u => u.Pastilles)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.CreatedByUserId);

        // Une pastille peut avoir plusieurs photos
        modelBuilder.Entity<Pastille>()
            .HasMany(p => p.Photos)
            .WithOne(photo => photo.Pastille)
            .HasForeignKey(photo => photo.PastilleId);

        // Un joueur ne peut noter une pastille qu'une seule fois
        modelBuilder.Entity<PastilleRating>()
            .HasIndex(r => new { r.PastilleId, r.RaterUserId })
            .IsUnique();

        // Une pastille peut avoir plusieurs quizzes
        modelBuilder.Entity<Pastille>()
            .HasMany(p => p.Quizzes) // Assure-toi d'ajouter 'public virtual ICollection<Quiz> Quizzes' à ton modèle Pastille.cs
            .WithOne(q => q.Pastille)
            .HasForeignKey(q => q.PastilleId);

        // Un quiz a plusieurs réponses
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Answers)
            .WithOne(a => a.Quiz)
            .HasForeignKey(a => a.QuizId);

        // Un joueur ne peut tenter un quiz qu'une seule fois (optionnel, mais recommandé)
        modelBuilder.Entity<UserQuizAttempt>()
            .HasIndex(a => new { a.UserId, a.QuizId })
            .IsUnique();
    }
}