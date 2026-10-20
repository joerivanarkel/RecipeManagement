using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public interface ICookLogService
{
    /// <returns>null if no recipe with that id existed</returns>
    Task<CookLogEntry?> RecordCookAsync(Guid recipeId, int servings, CancellationToken cancellationToken = default);

    Task<CookFrequency> GetFrequencyAsync(Guid recipeId, CancellationToken cancellationToken = default);

    Task<CookHistorySummary> GetHistoryAsync(Guid recipeId, CancellationToken cancellationToken = default);
}
