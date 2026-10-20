using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public record NewPantryItemInput(PantryCategory Category, string? ItemName, string? Quantity);
