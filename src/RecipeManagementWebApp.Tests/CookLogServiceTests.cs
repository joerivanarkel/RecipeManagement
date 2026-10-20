using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class CookLogServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipeDbContext _dbContext;

    public CookLogServiceTests()
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

    private async Task<Guid> CreateRecipeAsync(int servings = 4)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Test Recipe",
            Servings = servings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _dbContext.Recipes.Add(recipe);
        await _dbContext.SaveChangesAsync();
        return recipe.Id;
    }

    [Fact]
    public async Task RecordCookAsync_HappyPath_CreatesEntry()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);

        var entry = await service.RecordCookAsync(recipeId, 2);

        Assert.NotNull(entry);
        Assert.Equal(2, entry!.Servings);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Single(await _dbContext.CookLogEntries.ToListAsync());
    }

    [Fact]
    public async Task RecordCookAsync_InvalidServings_FallsBackToRecipeServings()
    {
        var recipeId = await CreateRecipeAsync(servings: 6);
        var service = new CookLogService(_dbContext);

        var entry = await service.RecordCookAsync(recipeId, 0);

        Assert.NotNull(entry);
        Assert.Equal(6, entry!.Servings);
    }

    [Fact]
    public async Task RecordCookAsync_UnknownRecipe_ReturnsNull()
    {
        var service = new CookLogService(_dbContext);

        var entry = await service.RecordCookAsync(Guid.NewGuid(), 2);

        Assert.Null(entry);
    }

    [Fact]
    public async Task GetFrequencyAsync_NoEntries_ReturnsZeroedFrequency()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);

        var frequency = await service.GetFrequencyAsync(recipeId);

        Assert.Equal(0, frequency.TotalCount);
        Assert.Null(frequency.LastCookedAt);
    }

    [Fact]
    public async Task GetFrequencyAsync_MultipleEntries_CalculatesTotalAndLastCooked()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);
        await service.RecordCookAsync(recipeId, 2);
        await service.RecordCookAsync(recipeId, 4);

        var frequency = await service.GetFrequencyAsync(recipeId);

        Assert.Equal(2, frequency.TotalCount);
        Assert.NotNull(frequency.LastCookedAt);
        Assert.Equal(2, frequency.FrequencyThisMonth);
    }

    [Fact]
    public async Task GetHistoryAsync_NoEntries_ReturnsEmptyWithZeroedStats()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);

        var history = await service.GetHistoryAsync(recipeId);

        Assert.Empty(history.Entries);
        Assert.Equal(0, history.TotalCount);
        Assert.Equal(0, history.CountThisMonth);
        Assert.Equal(0, history.CountThisWeek);
    }

    [Fact]
    public async Task GetHistoryAsync_MultipleEntries_OrderedMostRecentFirst()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);
        await service.RecordCookAsync(recipeId, 2);
        await Task.Delay(10);
        await service.RecordCookAsync(recipeId, 4);

        var history = await service.GetHistoryAsync(recipeId);

        Assert.Equal(2, history.TotalCount);
        Assert.Equal(2, history.Entries.Count);
        Assert.True(history.Entries[0].CookedAt >= history.Entries[1].CookedAt);
        Assert.Equal(4, history.Entries[0].Servings);
        Assert.Equal(2, history.Entries[1].Servings);
    }

    [Fact]
    public async Task RecipeDeletion_PreservesCookLogWithNullRecipeId()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new CookLogService(_dbContext);
        await service.RecordCookAsync(recipeId, 2);

        var recipe = await _dbContext.Recipes.SingleAsync(r => r.Id == recipeId);
        _dbContext.Recipes.Remove(recipe);
        await _dbContext.SaveChangesAsync();

        var entry = await _dbContext.CookLogEntries.SingleAsync();
        Assert.Null(entry.RecipeId);
    }
}
