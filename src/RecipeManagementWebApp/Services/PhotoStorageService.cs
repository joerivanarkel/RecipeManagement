using Microsoft.Extensions.Options;
using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class PhotoStorageService(IOptions<PhotoStorageOptions> options, ILogger<PhotoStorageService> logger)
    : IPhotoStorageService
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
    };

    private readonly PhotoStorageOptions _options = options.Value;

    public async Task<Result<Photo>> SavePhotoAsync(Guid recipeId, PhotoUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.TryGetValue(request.ContentType, out var extension))
        {
            return Result<Photo>.Failure("Invalid file format. Supported: JPG, PNG, GIF");
        }

        if (request.Size > MaxFileSizeBytes)
        {
            return Result<Photo>.Failure("File too large (max 5MB)");
        }

        var photoId = Guid.NewGuid();
        var storedFileName = $"{photoId}{extension}";

        try
        {
            Directory.CreateDirectory(_options.RootPath);
            var fullPath = Path.Combine(_options.RootPath, storedFileName);

            await using var fileStream = File.Create(fullPath);
            await request.Content.CopyToAsync(fileStream, cancellationToken);

            var photo = new Photo
            {
                Id = photoId,
                RecipeId = recipeId,
                Filename = request.FileName,
                MimeType = request.ContentType,
                Size = request.Size,
                StoredAt = $"{_options.RequestPathPrefix.TrimEnd('/')}/{storedFileName}",
                UploadedAt = DateTime.UtcNow,
            };

            return Result<Photo>.Success(photo);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Photo upload failed due to a storage error for recipe {RecipeId}", recipeId);
            return Result<Photo>.Failure("Recipe saved, but photo upload failed. You can add a photo later.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Photo upload failed due to a storage permission error for recipe {RecipeId}", recipeId);
            return Result<Photo>.Failure("Recipe saved, but photo upload failed. You can add a photo later.");
        }
    }

    public void DeletePhotoFile(Photo photo)
    {
        try
        {
            var fullPath = Path.Combine(_options.RootPath, Path.GetFileName(photo.StoredAt));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to delete stored photo file for photo {PhotoId}", photo.Id);
        }
    }
}
