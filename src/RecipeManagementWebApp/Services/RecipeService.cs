using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class RecipeService(RecipeDbContext dbContext, IPhotoStorageService photoStorageService) : IRecipeService
{
    public async Task<Result<CreateRecipeOutcome>> CreateAsync(
        NewRecipeInput input, PhotoUploadRequest? photo, CancellationToken cancellationToken = default)
    {
        var validation = RecipeValidator.Validate(input);
        if (!validation.Succeeded)
        {
            return Result<CreateRecipeOutcome>.Failure(validation.Errors);
        }

        var validated = validation.Value!;
        var recipeId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var recipe = new Recipe
        {
            Id = recipeId,
            Title = validated.Title,
            Servings = validated.Servings,
            CreatedAt = now,
            UpdatedAt = now,
            Ingredients = validated.Ingredients
                .Select((ingredient, index) => new Ingredient
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipeId,
                    Quantity = ingredient.Quantity,
                    Unit = ingredient.Unit,
                    Item = ingredient.Item,
                    Note = ingredient.Note,
                    DisplayOrder = index,
                })
                .ToList(),
            Steps = validated.Steps
                .Select((text, index) => new Step
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipeId,
                    Text = text,
                    DisplayOrder = index,
                })
                .ToList(),
        };

        string? photoWarning = null;
        if (photo is not null)
        {
            var photoResult = await photoStorageService.SavePhotoAsync(recipeId, photo, cancellationToken);
            if (photoResult.Succeeded)
            {
                recipe.Photo = photoResult.Value;
                recipe.PhotoId = photoResult.Value!.Id;
            }
            else
            {
                // Photo is optional — a failed upload must not block the recipe from saving (FR-013).
                photoWarning = photoResult.Errors.FirstOrDefault();
            }
        }

        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateRecipeOutcome>.Success(new CreateRecipeOutcome(recipe, photoWarning));
    }

    public async Task<IReadOnlyList<RecipeSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Recipes
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecipeSummary(r.Id, r.Title, r.Photo != null ? r.Photo.StoredAt : null, Array.Empty<string>()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Recipes
            .Include(r => r.Ingredients.OrderBy(i => i.DisplayOrder))
            .Include(r => r.Steps.OrderBy(s => s.DisplayOrder))
            .Include(r => r.Photo)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes.Include(r => r.Photo).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        if (recipe.Photo is not null)
        {
            photoStorageService.DeletePhotoFile(recipe.Photo);
        }

        dbContext.Recipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
