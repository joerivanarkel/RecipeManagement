using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class RecipeValidatorTests
{
    private static NewRecipeInput ValidInput() => new(
        Title: "Garlic Chicken Stir-Fry",
        Servings: "4",
        Ingredients:
        [
            new NewIngredientInput("2", "pieces", "chicken breast", ""),
            new NewIngredientInput("3", "cloves", "garlic", "minced"),
        ],
        Steps: ["Heat oil in a wok over high heat", "Add chicken and cook until golden"]);

    [Fact]
    public void Validate_HappyPath_Succeeds()
    {
        var result = RecipeValidator.Validate(ValidInput());

        Assert.True(result.Succeeded);
        Assert.Equal("Garlic Chicken Stir-Fry", result.Value!.Title);
        Assert.Equal(4, result.Value.Servings);
        Assert.Equal(2, result.Value.Ingredients.Count);
        Assert.Equal(2, result.Value.Steps.Count);
    }

    [Fact]
    public void Validate_MissingTitle_Fails()
    {
        var input = ValidInput() with { Title = "   " };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Title is required", result.Errors);
    }

    [Fact]
    public void Validate_TitleTooLong_Fails()
    {
        var input = ValidInput() with { Title = new string('a', 201) };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Title is too long (max 200 characters)", result.Errors);
    }

    [Fact]
    public void Validate_NonNumericServings_Fails()
    {
        var input = ValidInput() with { Servings = "abc" };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Servings must be a valid number", result.Errors);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Validate_NonPositiveServings_Fails(string servings)
    {
        var input = ValidInput() with { Servings = servings };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Servings must be greater than 0", result.Errors);
    }

    [Fact]
    public void Validate_NoIngredients_Fails()
    {
        var input = ValidInput() with { Ingredients = [] };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("At least one ingredient is required", result.Errors);
    }

    [Fact]
    public void Validate_IngredientMissingItem_Fails()
    {
        var input = ValidInput() with { Ingredients = [new NewIngredientInput("2", "cups", "", "")] };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Every ingredient must have an item", result.Errors);
    }

    [Fact]
    public void Validate_BlankIngredientRow_IsIgnoredNotError()
    {
        var input = ValidInput() with
        {
            Ingredients =
            [
                new NewIngredientInput("2", "pieces", "chicken breast", ""),
                new NewIngredientInput("", "", "", ""),
            ],
        };

        var result = RecipeValidator.Validate(input);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Ingredients);
    }

    [Fact]
    public void Validate_IngredientWithQuantityOnlyNoUnit_Succeeds()
    {
        var input = ValidInput() with
        {
            Ingredients = [new NewIngredientInput("1", "", "salt", "to taste")],
        };

        var result = RecipeValidator.Validate(input);

        Assert.True(result.Succeeded);
        var ingredient = Assert.Single(result.Value!.Ingredients);
        Assert.Equal(1, ingredient.Quantity);
        Assert.Null(ingredient.Unit);
        Assert.Equal("to taste", ingredient.Note);
    }

    [Fact]
    public void Validate_IngredientWithItemOnly_Succeeds()
    {
        var input = ValidInput() with
        {
            Ingredients = [new NewIngredientInput("", "", "lettuce", "")],
        };

        var result = RecipeValidator.Validate(input);

        Assert.True(result.Succeeded);
        var ingredient = Assert.Single(result.Value!.Ingredients);
        Assert.Null(ingredient.Quantity);
        Assert.Null(ingredient.Unit);
        Assert.Null(ingredient.Note);
        Assert.Equal("lettuce", ingredient.Item);
    }

    [Fact]
    public void Validate_WhitespaceOnlyStep_IsDroppedSilently()
    {
        var input = ValidInput() with { Steps = ["Heat oil", "   "] };

        var result = RecipeValidator.Validate(input);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Steps);
    }

    [Fact]
    public void Validate_StepTooLong_Fails()
    {
        var input = ValidInput() with { Steps = [new string('a', 1001)] };

        var result = RecipeValidator.Validate(input);

        Assert.False(result.Succeeded);
        Assert.Contains("Step text is too long (max 1000 characters)", result.Errors);
    }
}
