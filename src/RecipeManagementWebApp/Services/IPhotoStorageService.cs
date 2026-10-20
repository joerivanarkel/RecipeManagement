using RecipeManagementWebApp.Common;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public record PhotoUploadRequest(Stream Content, string FileName, string ContentType, long Size);

public interface IPhotoStorageService
{
    /// <summary>Validates format/size, then stores the file. Storage (IO) failures are returned as a Failure result rather than thrown, so a recipe save can proceed without a photo (FR-013).</summary>
    Task<Result<Photo>> SavePhotoAsync(Guid recipeId, PhotoUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Best-effort cleanup of the stored file when a recipe is deleted; failures are swallowed since the DB row is the source of truth.</summary>
    void DeletePhotoFile(Photo photo);
}
