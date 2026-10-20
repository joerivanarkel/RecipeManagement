using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Data;

public class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<Photo> Photos => Set<Photo>();

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
    }
}
