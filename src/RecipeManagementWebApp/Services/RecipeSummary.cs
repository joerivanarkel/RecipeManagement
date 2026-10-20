namespace RecipeManagementWebApp.Services;

/// <summary>Lightweight projection for the browse grid (spec 006).</summary>
public record RecipeSummary(
    Guid Id,
    string Title,
    string? PhotoUrl,
    IReadOnlyList<string> Tags,
    bool IsFavorite,
    IReadOnlyList<string> IngredientNames,
    DateTime? LastCookedAt);
