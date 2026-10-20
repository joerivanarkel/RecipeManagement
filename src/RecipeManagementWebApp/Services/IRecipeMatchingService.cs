namespace RecipeManagementWebApp.Services;

public interface IRecipeMatchingService
{
    /// <param name="extraIngredientNames">Session-only quick-entry ingredients (spec 013), unioned with the saved pantry.</param>
    Task<IReadOnlyList<RecipeAvailability>> GetRankingAsync(
        IReadOnlyCollection<string> extraIngredientNames, CancellationToken cancellationToken = default);
}
