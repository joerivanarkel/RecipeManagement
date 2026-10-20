using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class TagServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipeDbContext _dbContext;

    public TagServiceTests()
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

    private async Task<Guid> CreateRecipeAsync()
    {
        var service = new RecipeService(_dbContext, new NoopPhotoStorageService());
        var input = new NewRecipeInput(
            Title: "Garlic Chicken",
            Servings: "4",
            Ingredients: [new NewIngredientInput("2", "pieces", "chicken breast", "")],
            Steps: ["Cook it"]);
        var result = await service.CreateAsync(input, null);
        return result.Value!.Recipe.Id;
    }

    [Fact]
    public async Task AddTagAsync_NewTag_AddsAndNormalizesToLowercase()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new TagService(_dbContext);

        var result = await service.AddTagAsync(recipeId, "Vegetarian");

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        Assert.Contains("vegetarian", result.Value!);
    }

    [Fact]
    public async Task AddTagAsync_DuplicateCaseInsensitive_DoesNotDuplicate()
    {
        var recipeId = await CreateRecipeAsync();
        await new TagService(_dbContext).AddTagAsync(recipeId, "Quick");

        using var secondContext = NewContext();
        var result = await new TagService(secondContext).AddTagAsync(recipeId, "quick");

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task AddTagAsync_TooLong_ReturnsFailure()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new TagService(_dbContext);

        var result = await service.AddTagAsync(recipeId, new string('a', 51));

        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
    }

    [Fact]
    public async Task AddTagAsync_WhitespaceOnly_ReturnsFailure()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new TagService(_dbContext);

        var result = await service.AddTagAsync(recipeId, "   ");

        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
    }

    [Fact]
    public async Task AddTagAsync_UnknownRecipe_ReturnsNull()
    {
        var service = new TagService(_dbContext);

        var result = await service.AddTagAsync(Guid.NewGuid(), "vegetarian");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveTagAsync_ExistingTag_RemovesIt()
    {
        var recipeId = await CreateRecipeAsync();
        await new TagService(_dbContext).AddTagAsync(recipeId, "quick");

        using var secondContext = NewContext();
        var tagId = await secondContext.Tags.Where(t => t.RecipeId == recipeId).Select(t => t.Id).SingleAsync();
        var removed = await new TagService(secondContext).RemoveTagAsync(recipeId, tagId);

        Assert.True(removed);
        using var assertContext = NewContext();
        Assert.False(await assertContext.Tags.AnyAsync(t => t.RecipeId == recipeId));
    }

    [Fact]
    public async Task RemoveTagAsync_UnknownTag_ReturnsFalse()
    {
        var recipeId = await CreateRecipeAsync();
        var service = new TagService(_dbContext);

        var removed = await service.RemoveTagAsync(recipeId, Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task GetAllTagNamesAsync_ReturnsDistinctNamesAcrossRecipes()
    {
        var recipe1 = await CreateRecipeAsync();
        var recipe2 = await CreateRecipeAsync();
        await new TagService(_dbContext).AddTagAsync(recipe1, "vegetarian");

        using var secondContext = NewContext();
        await new TagService(secondContext).AddTagAsync(recipe2, "vegetarian");

        using var thirdContext = NewContext();
        await new TagService(thirdContext).AddTagAsync(recipe2, "quick");

        using var assertContext = NewContext();
        var names = await new TagService(assertContext).GetAllTagNamesAsync();

        Assert.Equal(["quick", "vegetarian"], names);
    }

    private class NoopPhotoStorageService : IPhotoStorageService
    {
        public Task<RecipeManagementWebApp.Common.Result<RecipeManagementWebApp.Domain.Photo>> SavePhotoAsync(
            Guid recipeId, PhotoUploadRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Photo storage was not expected to be called.");

        public void DeletePhotoFile(RecipeManagementWebApp.Domain.Photo photo) { }
    }
}
