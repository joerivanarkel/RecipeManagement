using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class PantryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipeDbContext _dbContext;

    public PantryServiceTests()
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

    [Fact]
    public async Task AddAsync_HappyPath_PersistsItem()
    {
        var service = new PantryService(_dbContext);

        var result = await service.AddAsync(new NewPantryItemInput(PantryCategory.Fridge, "chicken", "2 lbs"));

        Assert.True(result.Succeeded);
        var persisted = await _dbContext.PantryItems.SingleAsync();
        Assert.Equal("chicken", persisted.ItemName);
        Assert.Equal("2 lbs", persisted.Quantity);
        Assert.Equal(PantryCategory.Fridge, persisted.Category);
    }

    [Fact]
    public async Task AddAsync_EmptyItemName_ReturnsFailure()
    {
        var service = new PantryService(_dbContext);

        var result = await service.AddAsync(new NewPantryItemInput(PantryCategory.Fridge, "", "2 lbs"));

        Assert.False(result.Succeeded);
        Assert.Contains("Item name is required", result.Errors);
    }

    [Fact]
    public async Task AddAsync_EmptyQuantity_ReturnsFailure()
    {
        var service = new PantryService(_dbContext);

        var result = await service.AddAsync(new NewPantryItemInput(PantryCategory.Fridge, "chicken", ""));

        Assert.False(result.Succeeded);
        Assert.Contains("Quantity is required", result.Errors);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsItemsOrderedByCategoryThenAddedAt()
    {
        var service = new PantryService(_dbContext);
        await service.AddAsync(new NewPantryItemInput(PantryCategory.Freezer, "beef", "1 kg"));
        await service.AddAsync(new NewPantryItemInput(PantryCategory.Fridge, "milk", "1 liter"));

        var items = await service.GetAllAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(PantryCategory.Fridge, items[0].Category);
    }

    [Fact]
    public async Task UpdateQuantityAsync_ExistingItem_UpdatesQuantity()
    {
        var service = new PantryService(_dbContext);
        var created = await service.AddAsync(new NewPantryItemInput(PantryCategory.Cupboards, "flour", "3 lbs"));

        var result = await service.UpdateQuantityAsync(created.Value!.Id, "1.5 lbs");

        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        var persisted = await _dbContext.PantryItems.SingleAsync();
        Assert.Equal("1.5 lbs", persisted.Quantity);
    }

    [Fact]
    public async Task UpdateQuantityAsync_EmptyQuantity_ReturnsFailure()
    {
        var service = new PantryService(_dbContext);
        var created = await service.AddAsync(new NewPantryItemInput(PantryCategory.Cupboards, "flour", "3 lbs"));

        var result = await service.UpdateQuantityAsync(created.Value!.Id, "  ");

        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
    }

    [Fact]
    public async Task UpdateQuantityAsync_UnknownId_ReturnsNull()
    {
        var service = new PantryService(_dbContext);

        var result = await service.UpdateQuantityAsync(Guid.NewGuid(), "1 lb");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingItem_RemovesIt()
    {
        var service = new PantryService(_dbContext);
        var created = await service.AddAsync(new NewPantryItemInput(PantryCategory.Fridge, "eggs", "6"));

        var deleted = await service.DeleteAsync(created.Value!.Id);

        Assert.True(deleted);
        Assert.False(await _dbContext.PantryItems.AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var service = new PantryService(_dbContext);

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
