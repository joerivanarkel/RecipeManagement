using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class PantryService(RecipeDbContext dbContext) : IPantryService
{
    public async Task<Result<PantryItem>> AddAsync(NewPantryItemInput input, CancellationToken cancellationToken = default)
    {
        var validation = PantryItemValidator.Validate(input);
        if (!validation.Succeeded)
        {
            return Result<PantryItem>.Failure(validation.Errors);
        }

        var validated = validation.Value!;
        var now = DateTime.UtcNow;
        var item = new PantryItem
        {
            Id = Guid.NewGuid(),
            Category = input.Category,
            ItemName = validated.ItemName,
            Quantity = validated.Quantity,
            AddedAt = now,
            UpdatedAt = now,
        };

        dbContext.PantryItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PantryItem>.Success(item);
    }

    public async Task<IReadOnlyList<PantryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PantryItems
            .OrderBy(p => p.Category)
            .ThenBy(p => p.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<PantryItem>?> UpdateQuantityAsync(
        Guid id, string quantity, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PantryItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var trimmed = (quantity ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<PantryItem>.Failure("Quantity is required");
        }

        if (trimmed.Length > PantryItemValidator.QuantityMaxLength)
        {
            return Result<PantryItem>.Failure(
                $"Quantity is too long (max {PantryItemValidator.QuantityMaxLength} characters)");
        }

        item.Quantity = trimmed;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PantryItem>.Success(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PantryItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        dbContext.PantryItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
