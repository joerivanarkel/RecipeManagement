using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class RecipeMatchingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipeDbContext _dbContext;

    public RecipeMatchingServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = NewContext();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private RecipeDbContext NewContext() =>
        new(new DbContextOptionsBuilder<RecipeDbContext>().UseSqlite(_connection).Options);

    private Recipe AddRecipe(string title, params Ingredient[] ingredients)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = title,
            Servings = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Ingredients = ingredients.ToList(),
        };
        foreach (var ingredient in ingredients)
        {
            ingredient.RecipeId = recipe.Id;
        }

        _dbContext.Recipes.Add(recipe);
        _dbContext.SaveChanges();
        return recipe;
    }

    private static Ingredient Ingredient(string item, decimal? quantity = 1, string? note = null) => new()
    {
        Id = Guid.NewGuid(),
        Item = item,
        Quantity = quantity,
        Note = note,
    };

    private void AddPantryItem(string itemName)
    {
        _dbContext.PantryItems.Add(new PantryItem
        {
            Id = Guid.NewGuid(),
            Category = PantryCategory.Fridge,
            ItemName = itemName,
            Quantity = "some",
            AddedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetRankingAsync_RanksByAvailabilityDescending()
    {
        AddPantryItem("chicken");
        AddPantryItem("rice");
        AddPantryItem("garlic");

        AddRecipe("Chicken Fried Rice",
            Ingredient("chicken"), Ingredient("rice"), Ingredient("garlic"), Ingredient("soy sauce"), Ingredient("eggs"));
        AddRecipe("Garlic Bread", Ingredient("garlic"), Ingredient("bread"), Ingredient("butter"));
        AddRecipe("Pasta Carbonara", Ingredient("pasta"), Ingredient("eggs"), Ingredient("bacon"), Ingredient("parmesan"));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync([]);

        Assert.Equal(["Chicken Fried Rice", "Garlic Bread", "Pasta Carbonara"], ranking.Select(r => r.Title));
        Assert.Equal(3, ranking[0].AvailableCount);
        Assert.Equal(5, ranking[0].TotalCount);
        Assert.Equal(1, ranking[1].AvailableCount);
        Assert.Equal(0, ranking[2].AvailableCount);
    }

    [Fact]
    public async Task GetRankingAsync_IgnoresQuantityWhenMatching()
    {
        AddPantryItem("flour");
        AddPantryItem("eggs");
        AddRecipe("Pancakes", Ingredient("flour", quantity: 2), Ingredient("eggs", quantity: 12));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync([]);

        Assert.Equal(2, ranking[0].AvailableCount);
    }

    [Fact]
    public async Task GetRankingAsync_OpenEndedQuantity_IsAlwaysAvailable()
    {
        AddRecipe("Soup", Ingredient("salt", quantity: null, note: "to taste"), Ingredient("stock"));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync([]);

        Assert.Contains("salt", ranking[0].AvailableIngredients);
        Assert.Equal(1, ranking[0].AvailableCount);
    }

    [Fact]
    public async Task GetRankingAsync_CaseInsensitiveMatching()
    {
        AddPantryItem("Chicken");
        AddPantryItem("GARLIC");
        AddRecipe("Stir-Fry", Ingredient("chicken"), Ingredient("garlic"));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync([]);

        Assert.Equal(2, ranking[0].AvailableCount);
    }

    [Fact]
    public async Task GetRankingAsync_ExtraIngredients_SupplementPantry()
    {
        AddPantryItem("rice");
        AddRecipe("Chicken Cabbage Stir-Fry", Ingredient("chicken"), Ingredient("cabbage"), Ingredient("rice"));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync(["chicken", "cabbage"]);

        Assert.Equal(3, ranking[0].AvailableCount);
    }

    [Fact]
    public async Task GetRankingAsync_DuplicateBetweenPantryAndExtra_IsNotDoubleCounted()
    {
        AddPantryItem("chicken");
        AddRecipe("Chicken Soup", Ingredient("chicken"));

        var service = new RecipeMatchingService(NewContext());
        var ranking = await service.GetRankingAsync(["chicken"]);

        Assert.Equal(1, ranking[0].AvailableCount);
        Assert.Equal(1, ranking[0].TotalCount);
    }
}
