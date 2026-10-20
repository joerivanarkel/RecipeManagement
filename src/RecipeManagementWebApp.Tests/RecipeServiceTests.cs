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

        _dbContext = NewContext();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>A fresh DbContext over the same in-memory connection, mirroring the per-request scoped
    /// DbContext each HTTP request gets in production (rather than one context tracking an entire test).</summary>
    private RecipeDbContext NewContext() =>
        new(new DbContextOptionsBuilder<RecipeDbContext>().UseSqlite(_connection).Options);

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
        Assert.All(summaries, s => Assert.False(s.IsFavorite));
    }

    [Fact]
    public async Task GetSummariesAsync_ReflectsLastCookedAt()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));
        var created = await service.CreateAsync(ValidInput(), null);

        using var cookContext = NewContext();
        var cookLogService = new CookLogService(cookContext);
        await cookLogService.RecordCookAsync(created.Value!.Recipe.Id, 2);

        using var summaryContext = NewContext();
        var summaries = await new RecipeService(summaryContext, new StubPhotoStorageService(null)).GetSummariesAsync();

        Assert.NotNull(Assert.Single(summaries).LastCookedAt);
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

    [Fact]
    public async Task UpdateAsync_HappyPath_UpdatesFieldsAndBumpsUpdatedAt()
    {
        var created = await new RecipeService(_dbContext, new StubPhotoStorageService(null)).CreateAsync(ValidInput(), null);
        var recipeId = created.Value!.Recipe.Id;
        var originalUpdatedAt = created.Value.Recipe.UpdatedAt;

        var editedInput = ValidInput() with
        {
            Title = "Garlic Chicken with Rice",
            Servings = "6",
            Ingredients = [new NewIngredientInput("1", "tbsp", "ginger", "")],
            Steps = ["Simmer 8 minutes"],
        };

        using var updateContext = NewContext();
        var result = await new RecipeService(updateContext, new StubPhotoStorageService(null))
            .UpdateAsync(recipeId, editedInput, null, removePhoto: false);

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);

        using var assertContext = NewContext();
        var persisted = await assertContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .SingleAsync(r => r.Id == recipeId);
        Assert.Equal("Garlic Chicken with Rice", persisted.Title);
        Assert.Equal(6, persisted.Servings);
        Assert.Single(persisted.Ingredients);
        Assert.Equal("ginger", persisted.Ingredients[0].Item);
        Assert.Single(persisted.Steps);
        Assert.True(persisted.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_InvalidEdit_RejectsAndDoesNotPersist()
    {
        var created = await new RecipeService(_dbContext, new StubPhotoStorageService(null)).CreateAsync(ValidInput(), null);
        var recipeId = created.Value!.Recipe.Id;
        var originalTitle = created.Value.Recipe.Title;

        var invalidInput = ValidInput() with { Ingredients = [] };

        using var updateContext = NewContext();
        var result = await new RecipeService(updateContext, new StubPhotoStorageService(null))
            .UpdateAsync(recipeId, invalidInput, null, removePhoto: false);

        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
        Assert.Contains("At least one ingredient is required", result.Errors);

        using var assertContext = NewContext();
        var persisted = await assertContext.Recipes.SingleAsync(r => r.Id == recipeId);
        Assert.Equal(originalTitle, persisted.Title);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        var service = new RecipeService(_dbContext, new StubPhotoStorageService(null));

        var result = await service.UpdateAsync(Guid.NewGuid(), ValidInput(), null, removePhoto: false);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingPhoto()
    {
        var oldPhoto = new Photo
        {
            Id = Guid.NewGuid(), Filename = "old.jpg", MimeType = "image/jpeg", Size = 10,
            StoredAt = "/uploads/old.jpg", UploadedAt = DateTime.UtcNow,
        };
        var newPhoto = new Photo
        {
            Id = Guid.NewGuid(), Filename = "new.jpg", MimeType = "image/jpeg", Size = 20,
            StoredAt = "/uploads/new.jpg", UploadedAt = DateTime.UtcNow,
        };
        var createPhotoStorage = new StubPhotoStorageService(Result<Photo>.Success(oldPhoto));
        var createPhotoRequest = new PhotoUploadRequest(new MemoryStream([1]), "old.jpg", "image/jpeg", 10);
        var created = await new RecipeService(_dbContext, createPhotoStorage).CreateAsync(ValidInput(), createPhotoRequest);
        var recipeId = created.Value!.Recipe.Id;

        var updatePhotoStorage = new StubPhotoStorageService(Result<Photo>.Success(newPhoto));
        var updatePhotoRequest = new PhotoUploadRequest(new MemoryStream([2]), "new.jpg", "image/jpeg", 20);
        using var updateContext = NewContext();
        var result = await new RecipeService(updateContext, updatePhotoStorage)
            .UpdateAsync(recipeId, ValidInput(), updatePhotoRequest, removePhoto: false);

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        using var assertContext = NewContext();
        var persisted = await assertContext.Recipes.Include(r => r.Photo).SingleAsync(r => r.Id == recipeId);
        Assert.Equal(newPhoto.Id, persisted.PhotoId);
        Assert.Contains(oldPhoto.Id, updatePhotoStorage.DeletedPhotoIds);
    }

    [Fact]
    public async Task UpdateAsync_RemovePhoto_ClearsPhotoReference()
    {
        var photo = new Photo
        {
            Id = Guid.NewGuid(), Filename = "dish.jpg", MimeType = "image/jpeg", Size = 10,
            StoredAt = "/uploads/dish.jpg", UploadedAt = DateTime.UtcNow,
        };
        var createPhotoStorage = new StubPhotoStorageService(Result<Photo>.Success(photo));
        var photoRequest = new PhotoUploadRequest(new MemoryStream([1]), "dish.jpg", "image/jpeg", 10);
        var created = await new RecipeService(_dbContext, createPhotoStorage).CreateAsync(ValidInput(), photoRequest);
        var recipeId = created.Value!.Recipe.Id;

        var updatePhotoStorage = new StubPhotoStorageService(null);
        using var updateContext = NewContext();
        var result = await new RecipeService(updateContext, updatePhotoStorage)
            .UpdateAsync(recipeId, ValidInput(), null, removePhoto: true);

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        using var assertContext = NewContext();
        var persisted = await assertContext.Recipes.SingleAsync(r => r.Id == recipeId);
        Assert.Null(persisted.PhotoId);
        Assert.Contains(photo.Id, updatePhotoStorage.DeletedPhotoIds);
    }

    private class StubPhotoStorageService(Result<Photo>? result) : IPhotoStorageService
    {
        public List<Guid> DeletedPhotoIds { get; } = [];

        /// <summary>When set, overrides the result returned by the next SavePhotoAsync call (for multi-call scenarios like replace-photo).</summary>
        public Result<Photo>? NextResult { get; set; }

        public Task<Result<Photo>> SavePhotoAsync(
            Guid recipeId, PhotoUploadRequest request, CancellationToken cancellationToken = default)
        {
            var toReturn = NextResult ?? result ?? throw new InvalidOperationException("Photo storage was not expected to be called.");
            NextResult = null;
            return Task.FromResult(toReturn);
        }

        public void DeletePhotoFile(Photo photo) => DeletedPhotoIds.Add(photo.Id);
    }
}
