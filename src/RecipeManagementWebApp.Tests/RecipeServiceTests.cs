using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class RecipeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipeDbContext _dbContext;

    public RecipeServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new RecipeDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

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
    public async Task CreateAsync_HappyPath_PersistsRecipeWithIngredientsAndSteps()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));

        var result = await service.CreateAsync(ValidInput(), null);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.PhotoWarning);

        var persisted = await _dbContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .SingleAsync();
        Assert.Equal("Garlic Chicken Stir-Fry", persisted.Title);
        Assert.Equal(4, persisted.Servings);
        Assert.Equal(2, persisted.Ingredients.Count);
        Assert.Equal(2, persisted.Steps.Count);
        Assert.Null(persisted.PhotoId);
    }

    [Fact]
    public async Task CreateAsync_InvalidInput_DoesNotPersistAndReturnsErrors()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));
        var input = ValidInput() with { Title = "" };

        var result = await service.CreateAsync(input, null);

        Assert.False(result.Succeeded);
        Assert.Contains("Title is required", result.Errors);
        Assert.False(await _dbContext.Recipes.AnyAsync());
    }

    [Fact]
    public async Task CreateAsync_PhotoProvided_LinksPhotoToRecipe()
    {
        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            Filename = "dish.jpg",
            MimeType = "image/jpeg",
            Size = 1024,
            StoredAt = "/uploads/dish.jpg",
            UploadedAt = DateTime.UtcNow,
        };
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(Result<Photo>.Success(photo)));
        var photoRequest = new PhotoUploadRequest(new MemoryStream([1, 2, 3]), "dish.jpg", "image/jpeg", 1024);

        var result = await service.CreateAsync(ValidInput(), photoRequest);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.PhotoWarning);
        var persisted = await _dbContext.Recipes.Include(r => r.Photo).SingleAsync();
        Assert.NotNull(persisted.PhotoId);
        Assert.Equal(photo.Id, persisted.PhotoId);
    }

    [Fact]
    public async Task CreateAsync_PhotoUploadFails_RecipeStillSavesWithWarning()
    {
        var service = new RecipeService(
            _dbContext, new StubPhotoStorageService(Result<Photo>.Failure("Recipe saved, but photo upload failed. You can add a photo later.")));
        var photoRequest = new PhotoUploadRequest(new MemoryStream([1, 2, 3]), "dish.jpg", "image/jpeg", 1024);

        var result = await service.CreateAsync(ValidInput(), photoRequest);

        Assert.True(result.Succeeded);
        Assert.Equal("Recipe saved, but photo upload failed. You can add a photo later.", result.Value!.PhotoWarning);
        var persisted = await _dbContext.Recipes.SingleAsync();
        Assert.Null(persisted.PhotoId);
    }

    [Fact]
    public async Task GetSummariesAsync_ReturnsAllRecipesWithPhotoUrlAndEmptyTags()
    {
        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            Filename = "dish.jpg",
            MimeType = "image/jpeg",
            Size = 1024,
            StoredAt = "/uploads/dish.jpg",
            UploadedAt = DateTime.UtcNow,
        };
        var withPhoto = new RecipeService(_dbContext, new StubPhotoStorageService(Result<Photo>.Success(photo)));
        var photoRequest = new PhotoUploadRequest(new MemoryStream([1, 2, 3]), "dish.jpg", "image/jpeg", 1024);
        await withPhoto.CreateAsync(ValidInput(), photoRequest);

        var withoutPhoto = new RecipeService(_dbContext, new StubPhotoStorageService(null));
        await withoutPhoto.CreateAsync(ValidInput() with { Title = "Simple Pasta" }, null);

        var summaries = await withoutPhoto.GetSummariesAsync();

        Assert.Equal(2, summaries.Count);
        var withPhotoSummary = Assert.Single(summaries, s => s.Title == "Garlic Chicken Stir-Fry");
        Assert.Equal("/uploads/dish.jpg", withPhotoSummary.PhotoUrl);
        var withoutPhotoSummary = Assert.Single(summaries, s => s.Title == "Simple Pasta");
        Assert.Null(withoutPhotoSummary.PhotoUrl);
        Assert.All(summaries, s => Assert.Empty(s.Tags));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRecipe_ReturnsFullyPopulatedRecipe()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));
        var created = await service.CreateAsync(ValidInput(), null);

        var recipe = await service.GetByIdAsync(created.Value!.Recipe.Id);

        Assert.NotNull(recipe);
        Assert.Equal("Garlic Chicken Stir-Fry", recipe!.Title);
        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Equal(2, recipe.Steps.Count);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));

        var recipe = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(recipe);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRecipe_RemovesItAndCleansUpPhoto()
    {
        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            Filename = "dish.jpg",
            MimeType = "image/jpeg",
            Size = 1024,
            StoredAt = "/uploads/dish.jpg",
            UploadedAt = DateTime.UtcNow,
        };
        var photoStorage = new StubPhotoStorageService(Result<Photo>.Success(photo));
        var service = new RecipeService(_dbContext, photoStorage);
        var photoRequest = new PhotoUploadRequest(new MemoryStream([1, 2, 3]), "dish.jpg", "image/jpeg", 1024);
        var created = await service.CreateAsync(ValidInput(), photoRequest);

        var deleted = await service.DeleteAsync(created.Value!.Recipe.Id);

        Assert.True(deleted);
        Assert.False(await _dbContext.Recipes.AnyAsync());
        Assert.Contains(photo.Id, photoStorage.DeletedPhotoIds);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    private class StubPhotoStorageService(Result<Photo>? result) : IPhotoStorageService
    {
        public List<Guid> DeletedPhotoIds { get; } = [];

        public Task<Result<Photo>> SavePhotoAsync(
            Guid recipeId, PhotoUploadRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result ?? throw new InvalidOperationException("Photo storage was not expected to be called."));
        }

        public void DeletePhotoFile(Photo photo) => DeletedPhotoIds.Add(photo.Id);
    }
}
