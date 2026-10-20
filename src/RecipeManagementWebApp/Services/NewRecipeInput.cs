namespace RecipeManagementWebApp.Services;

public record NewRecipeInput(
    string Title,
    string Servings,
    IReadOnlyList<NewIngredientInput> Ingredients,
    IReadOnlyList<string> Steps);

public record NewIngredientInput(string Quantity, string Unit, string Item, string Note);
