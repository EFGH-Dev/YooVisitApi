using Microsoft.EntityFrameworkCore;
using YooVisitAPI.Models; // Assure-toi que ce namespace est correct

namespace YooVisitAPI.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

    // On déclare toutes les tables de notre royaume
    public DbSet<UserApplication> Users { get; set; }
    public DbSet<Pastille> Pastilles { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<PastilleRating> PastilleRatings { get; set; }
    public DbSet<Zone> Zones { get; set; }

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
    }
}