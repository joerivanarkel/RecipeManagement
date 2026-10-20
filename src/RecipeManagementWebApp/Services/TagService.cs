using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class TagService(RecipeDbContext dbContext) : ITagService
{
    public const int NameMaxLength = 50;

    public async Task<Result<IReadOnlyList<string>>?> AddTagAsync(
        Guid recipeId, string name, CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var normalized = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return Result<IReadOnlyList<string>>.Failure("Tag cannot be empty");
        }

        if (normalized.Length > NameMaxLength)
        {
            return Result<IReadOnlyList<string>>.Failure($"Tag is too long (max {NameMaxLength} characters)");
        }

        if (recipe.Tags.Any(t => t.Name == normalized))
        {
            return Result<IReadOnlyList<string>>.Success(recipe.Tags.Select(t => t.Name).ToList());
        }

        var tag = new Tag { Id = Guid.NewGuid(), RecipeId = recipeId, Name = normalized };
        dbContext.Tags.Add(tag);
        recipe.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<string>>.Success(recipe.Tags.Select(t => t.Name).ToList());
    }

    public async Task<bool> RemoveTagAsync(Guid recipeId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.RecipeId == recipeId, cancellationToken);
        if (tag is null)
        {
            return false;
        }

        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<string>> GetAllTagNamesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tags
            .Select(t => t.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
    }
}
