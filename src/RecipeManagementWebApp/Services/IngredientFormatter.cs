using System.Globalization;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

/// <summary>Formats an ingredient as prose, scaling its quantity for a target serving size (spec 007 FR-004, FR-007, FR-008).</summary>
public static class IngredientFormatter
{
    public static string FormatLine(Ingredient ingredient, int originalServings, int targetServings)
    {
        var scaleFactor = (decimal)targetServings / originalServings;

        var parts = new List<string>();
        if (ingredient.Quantity is { } quantity)
        {
            parts.Add(FormatQuantity(quantity * scaleFactor));
        }

        if (!string.IsNullOrWhiteSpace(ingredient.Unit))
        {
            parts.Add(ingredient.Unit);
        }

        parts.Add(ingredient.Item);

        var line = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(ingredient.Note) ? line : $"{line}, {ingredient.Note}";
    }

    public static string FormatQuantity(decimal value)
    {
        value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return value == Math.Floor(value)
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
