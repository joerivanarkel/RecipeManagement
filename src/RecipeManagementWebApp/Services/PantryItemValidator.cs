using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public record ValidatedPantryItem(string ItemName, string Quantity);

/// <summary>Validates and normalizes raw pantry item input per spec 012 FR-002/FR-003.</summary>
public static class PantryItemValidator
{
    public const int ItemNameMaxLength = 100;
    public const int QuantityMaxLength = 100;

    public static Result<ValidatedPantryItem> Validate(NewPantryItemInput input)
    {
        var errors = new List<string>();

        var itemName = (input.ItemName ?? string.Empty).Trim();
        if (itemName.Length == 0)
        {
            errors.Add("Item name is required");
        }
        else if (itemName.Length > ItemNameMaxLength)
        {
            errors.Add($"Item name is too long (max {ItemNameMaxLength} characters)");
        }

        var quantity = (input.Quantity ?? string.Empty).Trim();
        if (quantity.Length == 0)
        {
            errors.Add("Quantity is required");
        }
        else if (quantity.Length > QuantityMaxLength)
        {
            errors.Add($"Quantity is too long (max {QuantityMaxLength} characters)");
        }

        if (errors.Count > 0)
        {
            return Result<ValidatedPantryItem>.Failure(errors);
        }

        return Result<ValidatedPantryItem>.Success(new ValidatedPantryItem(itemName, quantity));
    }
}
