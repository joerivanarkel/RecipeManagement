using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class RecipeMatchingService(RecipeDbContext dbContext) : IRecipeMatchingService
{
    public async Task<IReadOnlyList<RecipeAvailability>> GetRankingAsync(
        IReadOnlyCollection<string> extraIngredientNames, CancellationToken cancellationToken = default)
    {
        var pantryNames = await dbContext.PantryItems
            .Select(p => p.ItemName)
            .ToListAsync(cancellationToken);

        var availableNames = pantryNames
            .Concat(extraIngredientNames)
            .Select(Normalize)
            .Where(n => n.Length > 0)
            .ToHashSet();

        var recipes = await dbContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Tags)
            .Include(r => r.Photo)
            .ToListAsync(cancellationToken);

        var lastCookedByRecipe = await dbContext.CookLogEntries
            .Where(c => c.RecipeId != null)
            .GroupBy(c => c.RecipeId!.Value)
            .Select(g => new { RecipeId = g.Key, LastCookedAt = g.Max(c => c.CookedAt) })
            .ToDictionaryAsync(x => x.RecipeId, x => (DateTime?)x.LastCookedAt, cancellationToken);

        var results = recipes
            .Select(recipe =>
            {
                var available = new List<string>();
                var missing = new List<string>();

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (IsOpenEnded(ingredient) || availableNames.Contains(Normalize(ingredient.Item)))
                    {
                        available.Add(ingredient.Item);
                    }
                    else
                    {
                        missing.Add(ingredient.Item);
                    }
                }

                return new RecipeAvailability(
                    RecipeId: recipe.Id,
                    Title: recipe.Title,
                    PhotoUrl: recipe.Photo?.StoredAt,
                    AvailableCount: available.Count,
                    TotalCount: recipe.Ingredients.Count,
                    AvailableIngredients: available,
                    MissingIngredients: missing,
                    Tags: recipe.Tags.Select(t => t.Name).ToList(),
                    LastCookedAt: lastCookedByRecipe.GetValueOrDefault(recipe.Id));
            })
            .OrderByDescending(r => r.PercentageAvailable)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return results;
    }

    private static bool IsOpenEnded(Ingredient ingredient) =>
        ingredient.Quantity is null
        && ingredient.Note is not null
        && (ingredient.Note.Contains("to taste", StringComparison.OrdinalIgnoreCase)
            || ingredient.Note.Contains("as needed", StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
