using RecipeManagementWebApp.Domain;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class IngredientFormatterTests
{
    private static Ingredient Ingredient(decimal? quantity, string? unit, string item, string? note) => new()
    {
        Id = Guid.NewGuid(),
        RecipeId = Guid.NewGuid(),
        Quantity = quantity,
        Unit = unit,
        Item = item,
        Note = note,
    };

    [Fact]
    public void FormatLine_QuantityUnitItemNote_FormatsAsProse()
    {
        var ingredient = Ingredient(2, "cups", "flour", "sifted");

        var line = IngredientFormatter.FormatLine(ingredient, originalServings: 2, targetServings: 2);

        Assert.Equal("2 cups flour, sifted", line);
    }

    [Fact]
    public void FormatLine_QuantityOnlyNoUnit_ScalesCorrectly()
    {
        var ingredient = Ingredient(2, null, "chicken breasts", null);

        var line = IngredientFormatter.FormatLine(ingredient, originalServings: 2, targetServings: 4);

        Assert.Equal("4 chicken breasts", line);
    }

    [Fact]
    public void FormatLine_ItemOnly_IsNotScaled()
    {
        var ingredient = Ingredient(null, null, "lettuce", null);

        var line = IngredientFormatter.FormatLine(ingredient, originalServings: 2, targetServings: 8);

        Assert.Equal("lettuce", line);
    }

    [Fact]
    public void FormatLine_NoQuantity_RemainsUnscaledWithNote()
    {
        var ingredient = Ingredient(null, null, "salt", "to taste");

        var line = IngredientFormatter.FormatLine(ingredient, originalServings: 4, targetServings: 1);

        Assert.Equal("salt, to taste", line);
    }

    [Fact]
    public void FormatLine_ScaleDown_ProducesDecimal()
    {
        var ingredient = Ingredient(2, null, "eggs", null);

        var line = IngredientFormatter.FormatLine(ingredient, originalServings: 4, targetServings: 1);

        Assert.Equal("0.5 eggs", line);
    }

    [Theory]
    [InlineData(4, "4")]
    [InlineData(1.5, "1.5")]
    [InlineData(0.5, "0.5")]
    [InlineData(3.333, "3.33")]
    public void FormatQuantity_FormatsReasonably(decimal value, string expected)
    {
        Assert.Equal(expected, IngredientFormatter.FormatQuantity(value));
    }
}
