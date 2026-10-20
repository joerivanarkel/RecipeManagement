using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public interface IUrlRecipeImportService
{
    Task<Result<ImportedRecipeDraft>> ImportAsync(string url, CancellationToken cancellationToken = default);
}
