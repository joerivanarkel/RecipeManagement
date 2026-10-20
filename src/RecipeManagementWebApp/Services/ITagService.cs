using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public interface ITagService
{
    /// <returns>null if no recipe with that id existed</returns>
    Task<Result<IReadOnlyList<string>>?> AddTagAsync(Guid recipeId, string name, CancellationToken cancellationToken = default);

    /// <returns>false if no recipe or tag with those ids existed</returns>
    Task<bool> RemoveTagAsync(Guid recipeId, Guid tagId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAllTagNamesAsync(CancellationToken cancellationToken = default);
}
