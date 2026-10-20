using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class PhotoStorageServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "recipe-photo-tests-" + Guid.NewGuid());
    private readonly PhotoStorageService _service;

    public PhotoStorageServiceTests()
    {
        var options = Options.Create(new PhotoStorageOptions { RootPath = _rootPath, RequestPathPrefix = "/uploads" });
        _service = new PhotoStorageService(options, NullLogger<PhotoStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SavePhotoAsync_ValidJpeg_SavesFileAndReturnsPhoto()
    {
        using var content = new MemoryStream([1, 2, 3, 4]);
        var request = new PhotoUploadRequest(content, "dish.jpg", "image/jpeg", content.Length);

        var result = await _service.SavePhotoAsync(Guid.NewGuid(), request);

        Assert.True(result.Succeeded);
        Assert.Equal("dish.jpg", result.Value!.Filename);
        Assert.StartsWith("/uploads/", result.Value.StoredAt);
        Assert.True(File.Exists(Path.Combine(_rootPath, Path.GetFileName(result.Value.StoredAt))));
    }

    [Fact]
    public async Task SavePhotoAsync_InvalidFormat_ReturnsFailureAndDoesNotWriteFile()
    {
        using var content = new MemoryStream([1, 2, 3]);
        var request = new PhotoUploadRequest(content, "notes.txt", "text/plain", content.Length);

        var result = await _service.SavePhotoAsync(Guid.NewGuid(), request);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid file format. Supported: JPG, PNG, GIF", result.Errors);
        Assert.False(Directory.Exists(_rootPath) && Directory.EnumerateFiles(_rootPath).Any());
    }

    [Fact]
    public async Task SavePhotoAsync_TooLarge_ReturnsFailure()
    {
        using var content = new MemoryStream(new byte[10]);
        var request = new PhotoUploadRequest(content, "dish.png", "image/png", PhotoStorageService.MaxFileSizeBytes + 1);

        var result = await _service.SavePhotoAsync(Guid.NewGuid(), request);

        Assert.False(result.Succeeded);
        Assert.Contains("File too large (max 5MB)", result.Errors);
    }
}
