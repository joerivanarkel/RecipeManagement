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

    public async Task<Result<CreateRecipeOutcome>?> UpdateAsync(
        Guid id, NewRecipeInput input, PhotoUploadRequest? newPhoto, bool removePhoto,
        CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Photo)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var validation = RecipeValidator.Validate(input);
        if (!validation.Succeeded)
        {
            return Result<CreateRecipeOutcome>.Failure(validation.Errors);
        }

        var validated = validation.Value!;

        recipe.Title = validated.Title;
        recipe.Servings = validated.Servings;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Explicit RemoveRange/AddRange (rather than mutating the navigation collection) is required so EF Core
        // marks the new rows as Added — a client-generated GUID key with no explicit Add() is otherwise treated
        // as an existing row (state Modified) rather than a new one.
        var newIngredients = validated.Ingredients
            .Select((ingredient, index) => new Ingredient
            {
                Id = Guid.NewGuid(),
                RecipeId = id,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit,
                Item = ingredient.Item,
                Note = ingredient.Note,
                DisplayOrder = index,
            })
            .ToList();
        dbContext.Ingredients.RemoveRange(recipe.Ingredients);
        dbContext.Ingredients.AddRange(newIngredients);
        recipe.Ingredients.Clear();
        recipe.Ingredients.AddRange(newIngredients);

        var newSteps = validated.Steps
            .Select((text, index) => new Step
            {
                Id = Guid.NewGuid(),
                RecipeId = id,
                Text = text,
                DisplayOrder = index,
            })
            .ToList();
        dbContext.Steps.RemoveRange(recipe.Steps);
        dbContext.Steps.AddRange(newSteps);
        recipe.Steps.Clear();
        recipe.Steps.AddRange(newSteps);

        string? photoWarning = null;
        Photo? photoFileToDeleteAfterSave = null;
        if (newPhoto is not null)
        {
            var photoResult = await photoStorageService.SavePhotoAsync(id, newPhoto, cancellationToken);
            if (photoResult.Succeeded)
            {
                if (recipe.Photo is not null)
                {
                    // Defer deleting the old file until the DB save succeeds, so a failed save
                    // doesn't leave the DB pointing at a photo file that's already gone (FR-005).
                    photoFileToDeleteAfterSave = recipe.Photo;
                    dbContext.Photos.Remove(recipe.Photo);
                }

                dbContext.Photos.Add(photoResult.Value!);
                recipe.Photo = photoResult.Value;
                recipe.PhotoId = photoResult.Value!.Id;
            }
            else
            {
                // Photo is optional — a failed upload must not block the rest of the edit from saving.
                photoWarning = photoResult.Errors.FirstOrDefault();
            }
        }
        else if (removePhoto && recipe.Photo is not null)
        {
            photoFileToDeleteAfterSave = recipe.Photo;
            dbContext.Photos.Remove(recipe.Photo);
            recipe.Photo = null;
            recipe.PhotoId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (photoFileToDeleteAfterSave is not null)
        {
            photoStorageService.DeletePhotoFile(photoFileToDeleteAfterSave);
        }

        return Result<CreateRecipeOutcome>.Success(new CreateRecipeOutcome(recipe, photoWarning));
    }

    public async Task<IReadOnlyList<RecipeSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Recipes
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecipeSummary(
                r.Id,
                r.Title,
                r.Photo != null ? r.Photo.StoredAt : null,
                r.Tags.Select(t => t.Name).ToList(),
                r.IsFavorite,
                r.Ingredients.Select(i => i.Item).ToList(),
                dbContext.CookLogEntries.Where(c => c.RecipeId == r.Id).Max(c => (DateTime?)c.CookedAt)))
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

        var photo = recipe.Photo;

        dbContext.Recipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (photo is not null)
        {
            photoStorageService.DeletePhotoFile(photo);
        }

        return true;
    }

    public async Task<bool?> ToggleFavoriteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        // Favouriting is not a content edit (spec 011 domain rule), so UpdatedAt is intentionally left untouched.
        recipe.IsFavorite = !recipe.IsFavorite;
        await dbContext.SaveChangesAsync(cancellationToken);
        return recipe.IsFavorite;
    }
}
