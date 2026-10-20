using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public interface IPantryService
{
    Task<Result<PantryItem>> AddAsync(NewPantryItemInput input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PantryItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <returns>null if no pantry item with that id existed</returns>
    Task<Result<PantryItem>?> UpdateQuantityAsync(
        Guid id, string quantity, CancellationToken cancellationToken = default);

    /// <returns>false if no pantry item with that id existed</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
