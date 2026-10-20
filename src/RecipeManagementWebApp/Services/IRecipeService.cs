using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

/// <summary>The saved recipe, plus a non-fatal warning when the optional photo couldn't be stored (FR-013).</summary>
public record CreateRecipeOutcome(Recipe Recipe, string? PhotoWarning);

public interface IRecipeService
{
    Task<Result<CreateRecipeOutcome>> CreateAsync(
        NewRecipeInput input, PhotoUploadRequest? photo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecipeSummary>> GetSummariesAsync(CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>false if no recipe with that id existed</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
