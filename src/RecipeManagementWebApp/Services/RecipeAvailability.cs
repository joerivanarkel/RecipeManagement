namespace RecipeManagementWebApp.Services;

/// <summary>How many of a recipe's ingredients are available given the current pantry (+ quick-entry) set (spec 014).</summary>
public record RecipeAvailability(
    Guid RecipeId,
    string Title,
    string? PhotoUrl,
    int AvailableCount,
    int TotalCount,
    IReadOnlyList<string> AvailableIngredients,
    IReadOnlyList<string> MissingIngredients,
    IReadOnlyList<string> Tags,
    DateTime? LastCookedAt)
{
    public double PercentageAvailable => TotalCount == 0 ? 0 : (double)AvailableCount / TotalCount;
}
