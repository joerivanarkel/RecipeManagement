using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Data;

public class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PantryItem> PantryItems => Set<PantryItem>();
    public DbSet<CookLogEntry> CookLogEntries => Set<CookLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Servings).IsRequired();

            entity.HasMany(r => r.Ingredients)
                .WithOne()
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Steps)
                .WithOne()
                .HasForeignKey(s => s.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Photo)
                .WithOne()
                .HasForeignKey<Photo>(p => p.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Tags)
                .WithOne()
                .HasForeignKey(t => t.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(r => r.IsFavorite).IsRequired();
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Item).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Unit).HasMaxLength(50);
            entity.Property(i => i.Note).HasMaxLength(300);
            entity.Property(i => i.Quantity).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Step>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Text).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Filename).IsRequired();
            entity.Property(p => p.MimeType).IsRequired();
            entity.Property(p => p.StoredAt).IsRequired();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<PantryItem>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Category).IsRequired();
            entity.Property(p => p.ItemName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Quantity).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<CookLogEntry>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CookedAt).IsRequired();
            entity.Property(c => c.Servings).IsRequired();

            // Cook log is an immutable audit trail (spec 023 FR-... / domain rule: "cook log preserved on delete"),
            // so recipe deletion must not cascade-delete history — RecipeId is nullable with SetNull instead.
            entity.HasOne<Recipe>()
                .WithMany()
                .HasForeignKey(c => c.RecipeId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
